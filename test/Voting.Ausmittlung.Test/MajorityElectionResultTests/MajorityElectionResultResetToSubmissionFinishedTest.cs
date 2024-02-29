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
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultResetToSubmissionFinishedTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultResetToSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultResettedToSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunToState(CountingCircleResultState.AuditedTentatively);
            await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultResettedToSubmissionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(
                new MajorityElectionResultResetToSubmissionFinishedRequest
                {
                    ElectionResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(
                new MajorityElectionResultResetToSubmissionFinishedRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(
                new MajorityElectionResultResetToSubmissionFinishedRequest
                {
                    ElectionResultId = MajorityElectionResultMockedData.IdKircheElectionResultInContestKirche,
                }),
            StatusCode.PermissionDenied);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        await RunOnDb(async db =>
        {
            var majorityElectionEndResult = await db.MajorityElectionEndResults.AsTracking().FirstAsync(x => x.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund));
            majorityElectionEndResult.Finalized = true;
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultResettedToSubmissionFinished
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                EventInfo = GetMockedEventInfo(),
            });
        await AssertCurrentState(CountingCircleResultState.SubmissionDone);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });

        endResult.Finalized.Should().BeFalse();
        endResult.MatchSnapshot();

        var id = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.SubmissionDone);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .ResetToSubmissionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private MajorityElectionResultResetToSubmissionFinishedRequest NewValidRequest()
    {
        return new MajorityElectionResultResetToSubmissionFinishedRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        };
    }
}
