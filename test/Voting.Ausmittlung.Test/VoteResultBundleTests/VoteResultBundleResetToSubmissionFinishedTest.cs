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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultBundleResetToSubmissionFinishedTest : VoteResultBundleBaseTest
{
    public VoteResultBundleResetToSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.Reviewed, VoteResultBundleMockedData.GossauBundle3.Id);
        await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleResetToSubmissionFinished>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunBundleToState(BallotBundleState.Reviewed, VoteResultBundleMockedData.GossauBundle3.Id);
            await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBundleResetToSubmissionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.Reviewed, VoteResultBundleMockedData.GossauBundle3.Id);
        await BundleErfassungElectionAdminClientStGallen.BundleResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleResetToSubmissionFinished>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await RunBundleToState(BallotBundleState.Reviewed, VoteResultBundleMockedData.GossauBundle3.Id);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.BundleResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowWhenResultSubmissionIsDone()
    {
        await RunBundleToState(BallotBundleState.Reviewed, VoteResultBundleMockedData.GossauBundle3.Id);
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "Cannot reset the bundle state in while the political business result is in state: SubmissionDone");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(
                new VoteResultBundleResetToSubmissionFinishedRequest
                {
                    BundleId = VoteResultBundleMockedData.IdUzwilBundle1,
                }),
            StatusCode.PermissionDenied,
            "This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Theory]
    [InlineData(BallotBundleState.InCorrection)]
    [InlineData(BallotBundleState.InProcess)]
    [InlineData(BallotBundleState.ReadyForReview)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await RunBundleToState(state, VoteResultBundleMockedData.GossauBundle3.Id);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await CreateBallot();
        await RunBundleToState(BallotBundleState.Reviewed);
        await RunAllEvents();

        var bundleBefore = await GetBundle();
        bundleBefore.State.Should().Be(BallotBundleState.Reviewed);
        bundleBefore.BallotResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(2);
        bundleBefore.BallotResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleResetToSubmissionFinished
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                SampleBallotNumbers = { 1 },
            });
        var bundleAfter = await GetBundle();
        bundleAfter.State.Should().Be(BallotBundleState.ReadyForReview);
        bundleAfter.BallotResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);
        bundleAfter.BallotResult.AllBundlesReviewedOrDeleted.Should().BeFalse();
        bundleAfter.BallotResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(3);

        foreach (var log in bundleAfter.Logs)
        {
            log.Id = Guid.Empty;
        }

        bundleAfter.MatchSnapshot(x => x.BallotResult.VoteResult.CountingCircleId);
        await AssertHasPublishedEventProcessedMessage(VoteResultBundleResetToSubmissionFinished.Descriptor, bundleAfter.Id);
    }

    [Fact]
    public async Task TestProcessorUpdatesQuestionResults()
    {
        await CreateBallot();
        await CreateBallot();
        await RunBundleToState(BallotBundleState.Reviewed);
        await RunAllEvents();

        await ShouldHaveQuestionResults(true);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleResetToSubmissionFinished
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                SampleBallotNumbers = { 1 },
            });

        await ShouldHaveQuestionResults(false);

        var questionResults = await RunOnDb(
            db => db.BallotQuestionResults
                .Include(x => x.Question.Translations)
                .Where(x => x.BallotResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult))
                .ToListAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(questionResults.SelectMany(x => x.Question.Translations));
        questionResults.MatchSnapshot("questionResults", x => x.Id);

        var tieBreakQuestionResults = await RunOnDb(
            db => db.TieBreakQuestionResults
                .Include(x => x.Question.Translations)
                .Where(x => x.BallotResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult))
                .ToListAsync(),
            Languages.Italian);
        SetDynamicIdToDefaultValue(tieBreakQuestionResults.SelectMany(x => x.Question.Translations));
        tieBreakQuestionResults.MatchSnapshot("tieBreakQuestionResults", x => x.Id);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var bundleId = await CreateBundle(10, "another-user");
        await RunBundleToState(BallotBundleState.Reviewed, bundleId);
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .BundleResetToSubmissionFinishedAsync(new VoteResultBundleResetToSubmissionFinishedRequest
            {
                BundleId = bundleId.ToString(),
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private VoteResultBundleResetToSubmissionFinishedRequest NewValidRequest()
    {
        return new VoteResultBundleResetToSubmissionFinishedRequest
        {
            BundleId = VoteResultBundleMockedData.IdGossauBundle3,
        };
    }
}
