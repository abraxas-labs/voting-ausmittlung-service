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
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultPlausibilisedTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultPlausibilisedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await MonitoringElectionAdminClient.PlausibiliseAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultPlausibilised>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunToState(CountingCircleResultState.AuditedTentatively);
            await MonitoringElectionAdminClient.PlausibiliseAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultPlausibilised>();
        });
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PlausibiliseAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(IdNotFound))),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PlausibiliseAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(IdBadFormat))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PlausibiliseAsync(
                new MajorityElectionResultsPlausibiliseRequest
                {
                    ElectionResultIds =
                    {
                            MajorityElectionResultMockedData.IdKircheElectionResultInContestKirche,
                    },
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PlausibiliseAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PlausibiliseAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PlausibiliseAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowStatePlausibilisedDisabled()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        await RunOnDb(async db =>
        {
            var cantonDefaults = await db.ContestCantonDefaults.AsTracking().AsSplitQuery().SingleAsync(x =>
                x.ContestId == Guid.Parse(ContestMockedData.IdBundesurnengang));

            cantonDefaults.StatePlausibilisedDisabled = true;
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.PlausibiliseAsync(
                NewValidRequest()),
            StatusCode.InvalidArgument,
            "state plausibilised is not enabled for contest");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        await MonitoringElectionAdminClient.PlausibiliseAsync(NewValidRequest());
        await RunEvents<MajorityElectionResultPlausibilised>();

        await AssertCurrentState(CountingCircleResultState.Plausibilised);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });

        endResult.MatchSnapshot();

        var id = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.Plausibilised);

        var resultEntity = await RunOnDb(db => db.MajorityElectionResults.SingleAsync(x => x.Id == id));
        resultEntity.PlausibilisedTimestamp.Should().NotBeNull();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .PlausibiliseAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private MajorityElectionResultsPlausibiliseRequest NewValidRequest(Action<MajorityElectionResultsPlausibiliseRequest>? customizer = null)
    {
        var r = new MajorityElectionResultsPlausibiliseRequest
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
