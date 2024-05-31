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

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultPublishTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultPublishTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await MonitoringElectionAdminClient.PublishAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultPublished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWhenPlausibilised()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await MonitoringElectionAdminClient.PublishAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultPublished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunToState(CountingCircleResultState.AuditedTentatively);
            await MonitoringElectionAdminClient.PublishAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultPublished>();
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
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
                NewValidRequest(x => x.ElectionResultIds.Add(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))),
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
                x.ContestId == ContestMockedData.GuidBundesurnengang);

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
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id && x.SnapshotContestId == ContestMockedData.GuidBundesurnengang,
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
        await RunEvents<MajorityElectionResultPublished>();

        var aggregate = await AggregateRepositoryMock.GetOrCreateById<MajorityElectionResultAggregate>(
            Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund));

        aggregate.Published.Should().BeTrue();

        var result = await MonitoringElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });

        result.Published.Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .PublishAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private MajorityElectionResultPublishRequest NewValidRequest(Action<MajorityElectionResultPublishRequest>? customizer = null)
    {
        var r = new MajorityElectionResultPublishRequest
        {
            ElectionResultIds =
                {
                    MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
