// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultPublishTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultPublishTest(TestApplicationFactory factory)
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
        await MonitoringElectionAdminClient.PublishAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultPublished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWhenPlausibilised()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await MonitoringElectionAdminClient.PublishAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultPublished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.AuditedTentatively);
            await MonitoringElectionAdminClient.PublishAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultPublished>();
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PublishAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PublishAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(IdNotFound))),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PublishAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(IdBadFormat))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PublishAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen))),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PublishAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowAlreadyPublished()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PublishAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "result is already published");
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
            async () => await MonitoringElectionAdminClient.PublishAsync(
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
            async () => await MonitoringElectionAdminClient.PublishAsync(
                NewValidRequest()),
            StatusCode.InvalidArgument,
            $"cannot publish results for domain of influence type {DomainOfInfluenceType.Mu} or lower");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        await MonitoringElectionAdminClient.PublishAsync(NewValidRequest());
        await RunEvents<ProportionalElectionResultPublished>();

        var aggregate = await AggregateRepositoryMock.GetOrCreateById<ProportionalElectionResultAggregate>(
            Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen));

        aggregate.Published.Should().BeTrue();

        var result = await MonitoringElectionAdminClient.GetAsync(new GetProportionalElectionResultRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });

        result.Published.Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .PublishAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private ProportionalElectionResultPublishRequest NewValidRequest(Action<ProportionalElectionResultPublishRequest>? customizer = null)
    {
        var r = new ProportionalElectionResultPublishRequest
        {
            ElectionResultIds =
                {
                    ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
