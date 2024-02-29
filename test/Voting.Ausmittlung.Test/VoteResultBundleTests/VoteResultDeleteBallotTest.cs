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

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultDeleteBallotTest : VoteResultBundleBaseTest
{
    public VoteResultDeleteBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await BundleErfassungElectionAdminClient.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanCreator()
    {
        await BundleErfassungElectionAdminClientSecondUser.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await BundleErfassungCreatorClient.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await BundleErfassungCreatorClient.DeleteBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBallotDeleted>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorInCorrection()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await BundleErfassungCreatorClient.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientStGallen.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.DeleteBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherUser()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.DeleteBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "only election admins or the creator of a bundle can edit it");
    }

    [Fact]
    public async Task TestShouldThrowInexistentBallotNumber()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.DeleteBallotAsync(NewValidRequest(x => x.BallotNumber = 99)),
            StatusCode.InvalidArgument,
            "only the last ballot can be deleted");
    }

    [Fact]
    public async Task TestShouldThrowWrongBallotNumber()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.DeleteBallotAsync(NewValidRequest(x => x.BallotNumber = 1)),
            StatusCode.InvalidArgument,
            "only the last ballot can be deleted");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.DeleteBallotAsync(new DeleteVoteResultBallotRequest
            {
                BundleId = VoteResultBundleMockedData.IdUzwilBundle1,
                BallotNumber = 1,
            }),
            StatusCode.PermissionDenied);
    }

    [Theory]
    [InlineData(BallotBundleState.ReadyForReview)]
    [InlineData(BallotBundleState.Reviewed)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await RunBundleToState(state);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.DeleteBallotAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.DeleteBallotAsync(NewValidRequest()),
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
            new VoteResultBallotDeleted
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                BallotNumber = 1,
                EventInfo = GetMockedEventInfo(),
            });
        var ballot = await RunOnDb(db => db.VoteResultBallots
            .FirstOrDefaultAsync(x => x.BundleId == Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1)
                             && x.Number == 1));
        ballot.Should().BeNull();

        var bundle = await GetBundle();
        bundle.CountOfBallots.Should().Be(1);
        bundle.BallotResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);

        await AssertHasPublishedMessage<VoteBundleChanged>(
            x => x.Id == bundle1Id && x.BallotResultId == ballotResultId);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .DeleteBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await CreateBallot();
        await CreateBallot();
    }

    private DeleteVoteResultBallotRequest NewValidRequest(
        Action<DeleteVoteResultBallotRequest>? customizer = null)
    {
        var req = new DeleteVoteResultBallotRequest
        {
            BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            BallotNumber = LatestBallotNumber,
        };
        customizer?.Invoke(req);
        return req;
    }
}
