// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultFlagForCorrectionTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultFlagForCorrectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminWithoutComment()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest(x => x.Comment = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithNoUnpublishWhenBeforeAuditedPublish()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidStGallenEvoting,
            x =>
            {
                x.PublishResultsBeforeAuditedTentatively = true;
            },
            splitQuery: true);

        await RunToState(CountingCircleResultState.SubmissionDone);
        await RunToPublished();

        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<VoteResultUnpublished>().Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionDone);
            await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultFlaggedForCorrection>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterCorrection()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
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
                new VoteResultFlagForCorrectionRequest
                {
                    VoteResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(
                new VoteResultFlagForCorrectionRequest
                {
                    VoteResultId = IdBadFormat,
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
        await AssertReadyForCorrectionTimestamp(false);

        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        await RunEvents<VoteResultFlaggedForCorrection>();

        await AssertCurrentState(CountingCircleResultState.ReadyForCorrection);
        await AssertSubmissionDoneTimestamp(false);
        await AssertReadyForCorrectionTimestamp(true);
        var comments = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult))
            .ToListAsync());
        comments.MatchSnapshot(x => x.Id);

        var id = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult);
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.ReadyForCorrection);
    }

    [Fact]
    public async Task TestProcessorWithoutComment()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest(req => req.Comment = string.Empty));
        await RunEvents<VoteResultFlaggedForCorrection>();

        await AssertCurrentState(CountingCircleResultState.ReadyForCorrection);
        var comment = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult))
            .FirstOrDefaultAsync());
        comment.Should().BeNull();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await new VoteResultService.VoteResultServiceClient(channel)
            .FlagForCorrectionAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private VoteResultFlagForCorrectionRequest NewValidRequest(
        Action<VoteResultFlagForCorrectionRequest>? customizer = null)
    {
        var req = new VoteResultFlagForCorrectionRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            Comment = "my-comment",
        };
        customizer?.Invoke(req);
        return req;
    }

    private async Task AssertSubmissionDoneTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.VoteResults.SingleAsync(x => x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult)));
        (result.SubmissionDoneTimestamp != null).Should().Be(hasTimestamp);
    }

    private async Task AssertReadyForCorrectionTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.VoteResults.SingleAsync(x => x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult)));
        (result.ReadyForCorrectionTimestamp != null).Should().Be(hasTimestamp);
    }
}
