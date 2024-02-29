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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultDeleteBundleTest : VoteResultBundleBaseTest
{
    public VoteResultDeleteBundleTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await CreateBallot();
        await BundleErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CreateBallot();
            await BundleErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest());
            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBundleDeleted>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBundleNumberFreed>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanBundleCreator()
    {
        await CreateBallot();
        await BundleErfassungElectionAdminClientSecondUser.DeleteBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Theory]
    [InlineData(BallotBundleState.InProcess)]
    [InlineData(BallotBundleState.InCorrection)]
    [InlineData(BallotBundleState.ReadyForReview)]
    [InlineData(BallotBundleState.Reviewed)]
    public async Task TestShouldReturnForStates(BallotBundleState state)
    {
        await RunBundleToState(state);
        await BundleErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await CreateBallot();
        await BundleErfassungElectionAdminClientStGallen.DeleteBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.DeleteBundleAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherThanBundleCreator()
    {
        await CreateBallot();
        await SetBundleDeleted();
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.DeleteBundleAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.DeleteBundleAsync(new DeleteVoteResultBundleRequest
            {
                BundleId = "c9be6337-d25d-4e29-84e6-551ad5752f64",
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.DeleteBundleAsync(new DeleteVoteResultBundleRequest
            {
                BundleId = VoteResultBundleMockedData.IdUzwilBundle1,
                BallotResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenBallotResult,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAlreadyDeleted()
    {
        await SetBundleDeleted();
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "bundle is already deleted");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest()),
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
            new VoteResultBundleDeleted
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.Deleted);
        bundle.BallotResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(1);
        bundle.BallotResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);

        await AssertHasPublishedMessage<VoteBundleChanged>(
            x => x.Id == bundle1Id && x.BallotResultId == ballotResultId);
    }

    [Fact]
    public async Task TestProcessorUpdatesQuestionResultsIfNotReviewedYet()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await ShouldHaveQuestionResults(false);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleDeleted
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        await ShouldHaveQuestionResults(false);
    }

    [Fact]
    public async Task TestProcessorUpdatesQuestionResults()
    {
        await RunBundleToState(BallotBundleState.Reviewed);
        await ShouldHaveQuestionResults(true);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleDeleted
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        await ShouldHaveQuestionResults(false);
    }

    [Fact]
    public async Task TestProcessorDoesntUpdateQuestionResultsIfInProcess()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleDeleted
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        await ShouldHaveQuestionResults(false);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .DeleteBundleAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private DeleteVoteResultBundleRequest NewValidRequest()
    {
        return new DeleteVoteResultBundleRequest
        {
            BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
        };
    }
}
