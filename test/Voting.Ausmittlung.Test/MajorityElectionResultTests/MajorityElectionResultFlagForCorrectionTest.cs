// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultFlagForCorrectionTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultFlagForCorrectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminWithoutComment()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest(x => x.Comment = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterCorrection()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionDone);
            await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultFlaggedForCorrection>();
        });
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(
                new MajorityElectionResultFlagForCorrectionRequest
                {
                    ElectionResultId = MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
                }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(
                new MajorityElectionResultFlagForCorrectionRequest
                {
                    ElectionResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(
                new MajorityElectionResultFlagForCorrectionRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertSubmissionDoneTimestamp(true);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultFlaggedForCorrection
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                Comment = "my-comment",
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });
        await AssertCurrentState(CountingCircleResultState.ReadyForCorrection);
        await AssertSubmissionDoneTimestamp(false);
        var comments = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))
            .ToListAsync());
        comments.MatchSnapshot(x => x.Id);

        var id = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.ReadyForCorrection);
    }

    [Fact]
    public async Task TestProcessorWithoutComment()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultFlaggedForCorrection
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                EventInfo = GetMockedEventInfo(),
            });
        await AssertCurrentState(CountingCircleResultState.ReadyForCorrection);
        var comment = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))
            .FirstOrDefaultAsync());
        comment.Should().BeNull();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .FlagForCorrectionAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private MajorityElectionResultFlagForCorrectionRequest NewValidRequest(
        Action<MajorityElectionResultFlagForCorrectionRequest>? customizer = null)
    {
        var req = new MajorityElectionResultFlagForCorrectionRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            Comment = "my-comment",
        };
        customizer?.Invoke(req);
        return req;
    }

    private async Task AssertSubmissionDoneTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.MajorityElectionResults.SingleAsync(x => x.Id == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund)));
        (result.SubmissionDoneTimestamp != null).Should().Be(hasTimestamp);
    }
}
