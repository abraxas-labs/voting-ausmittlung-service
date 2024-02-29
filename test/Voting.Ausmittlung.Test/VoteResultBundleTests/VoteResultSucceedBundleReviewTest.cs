// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultSucceedBundleReviewTest : VoteResultBundleBaseTest
{
    public VoteResultSucceedBundleReviewTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunBundleToState(BallotBundleState.ReadyForReview);
            await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBundleReviewSucceeded>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungElectionAdminClientStGallen.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorSameUserAsBundleCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "The creator of a bundle can't review it");
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungElectionAdminSameUserAsBundleCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "The creator of a bundle can't review it");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(
                new SucceedVoteBundleReviewRequest
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
            async () => await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Theory]
    [InlineData(BallotBundleState.InCorrection)]
    [InlineData(BallotBundleState.InProcess)]
    [InlineData(BallotBundleState.Reviewed)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await RunBundleToState(state);
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var ballotResultId = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult);
        var bundle1Id = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleReviewSucceeded
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            });
        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.Reviewed);
        bundle.BallotResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);
        bundle.BallotResult.AllBundlesReviewedOrDeleted.Should().BeFalse();
        bundle.BallotResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(1);
        bundle.MatchSnapshot(x => x.BallotResult.VoteResult.CountingCircleId);

        await AssertHasPublishedMessage<VoteBundleChanged>(
            x => x.Id == bundle1Id && x.BallotResultId == ballotResultId);
    }

    [Fact]
    public async Task TestProcessorUpdatesQuestionResults()
    {
        await CreateBallot();
        await CreateBallot();
        await CreateBallot(VoteResultBundleMockedData.IdGossauBundle2);

        await ShouldHaveQuestionResults(false);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleReviewSucceeded
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });

        await ShouldHaveQuestionResults(true);

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

        var result = await GetBallotResult();
        result.CountOfBundlesNotReviewedOrDeleted.Should().Be(1);
        result.AllBundlesReviewedOrDeleted.Should().BeFalse();
        result.ConventionalCountOfDetailedEnteredBallots.Should().Be(2);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .SucceedBundleReviewAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private SucceedVoteBundleReviewRequest NewValidRequest()
    {
        return new SucceedVoteBundleReviewRequest
        {
            BundleId = VoteResultBundleMockedData.IdGossauBundle1,
        };
    }
}
