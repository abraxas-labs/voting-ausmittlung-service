// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultRejectBundleReviewTest : VoteResultBundleBaseTest
{
    public VoteResultRejectBundleReviewTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.RejectBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungElectionAdminClientSecondUser.RejectBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunBundleToState(BallotBundleState.ReadyForReview);
            await BundleErfassungCreatorClientSecondUser.RejectBundleReviewAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBundleReviewRejected>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungElectionAdminClientStGallen.RejectBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.RejectBundleReviewAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorSameUserAsBundleCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.RejectBundleReviewAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "The creator of a bundle can't review it");
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungElectionAdminSameUserAsBundleCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.RejectBundleReviewAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "The creator of a bundle can't review it");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.RejectBundleReviewAsync(
                new RejectVoteBundleReviewRequest
                {
                    BundleId = VoteResultBundleMockedData.IdUzwilBundle1,
                }),
            StatusCode.PermissionDenied,
            "This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
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
            async () => await BundleErfassungCreatorClientSecondUser.RejectBundleReviewAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.RejectBundleReviewAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var ballotResultId = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult);
        var bundle1Id = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleReviewRejected
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            });
        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.InCorrection);
        bundle.BallotResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);
        bundle.BallotResult.AllBundlesReviewedOrDeleted.Should().BeFalse();
        bundle.BallotResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);
        bundle.MatchSnapshot(x => x.BallotResult.VoteResult.CountingCircleId);

        await AssertHasPublishedMessage<VoteBundleChanged>(
            x => x.Id == bundle1Id && x.BallotResultId == ballotResultId);
    }

    [Fact]
    public async Task TestProcessorUpdatesQuestionResults()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleReviewRejected
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            });

        await ShouldHaveQuestionResults(false);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .RejectBundleReviewAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private RejectVoteBundleReviewRequest NewValidRequest()
    {
        return new RejectVoteBundleReviewRequest
        {
            BundleId = VoteResultBundleMockedData.IdGossauBundle1,
        };
    }
}
