// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultUnpublishTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultUnpublishTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.Gossau.Id && x.SnapshotContestId == ContestMockedData.StGallenEvotingUrnengang.Id,
            x => x.Type = DomainOfInfluenceType.Bz);
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();
        await MonitoringElectionAdminClient.UnpublishAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultUnpublished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWhenPlausibilised()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await RunToPublished();
        await MonitoringElectionAdminClient.UnpublishAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultUnpublished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.AuditedTentatively);
            await RunToPublished();
            await MonitoringElectionAdminClient.UnpublishAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultUnpublished>();
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UnpublishAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UnpublishAsync(
                NewValidRequest(x => x.VoteResultIds.Add(IdNotFound))),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UnpublishAsync(
                NewValidRequest(x => x.VoteResultIds.Add(IdBadFormat))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UnpublishAsync(
                NewValidRequest(x => x.VoteResultIds.Add(VoteResultMockedData.IdGossauVoteInContestStGallenResult))),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    public async Task TestShouldReturnWithSubmissionDoneBeforeAuditedPublishCantonSettings(CountingCircleResultState state)
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidStGallenEvoting,
            x =>
            {
                x.PublishResultsBeforeAuditedTentatively = true;
            },
            splitQuery: true);

        await RunToState(state);
        await RunToPublished();
        await MonitoringElectionAdminClient.UnpublishAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<VoteResultUnpublished>().Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UnpublishAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "cannot publish or unpublish a result with the state");
    }

    [Fact]
    public async Task TestShouldThrowAlreadyUnpublished()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UnpublishAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "result is already unpublished");
    }

    [Fact]
    public async Task TestShouldThrowPublishResultsDisabled()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        await RunOnDb(async db =>
        {
            var cantonDefaults = await db.ContestCantonDefaults.AsSplitQuery().AsTracking().SingleAsync(x =>
                x.ContestId == Guid.Parse(ContestMockedData.IdStGallenEvoting));

            cantonDefaults.PublishResultsEnabled = false;
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UnpublishAsync(
                NewValidRequest()),
            StatusCode.InvalidArgument,
            "publish results is not enabled for contest");
    }

    [Fact]
    public async Task TestShouldThrowWrongDoiType()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.Gossau.Id && x.SnapshotContestId == ContestMockedData.StGallenEvotingUrnengang.Id,
            x => x.Type = DomainOfInfluenceType.Mu);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UnpublishAsync(
                NewValidRequest()),
            StatusCode.InvalidArgument,
            $"cannot publish or unpublish results for domain of influence type {DomainOfInfluenceType.Mu} or lower");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();

        await MonitoringElectionAdminClient.UnpublishAsync(NewValidRequest());
        await RunEvents<VoteResultUnpublished>();

        var aggregate = await AggregateRepositoryMock.GetOrCreateById<VoteResultAggregate>(
            Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult));

        aggregate.Published.Should().BeFalse();

        var result = await MonitoringElectionAdminClient.GetAsync(new GetVoteResultRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
        });

        result.Published.Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();
        await new VoteResultService.VoteResultServiceClient(channel)
            .UnpublishAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private VoteResultUnpublishRequest NewValidRequest(Action<VoteResultUnpublishRequest>? customizer = null)
    {
        var r = new VoteResultUnpublishRequest
        {
            VoteResultIds =
                {
                    VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
