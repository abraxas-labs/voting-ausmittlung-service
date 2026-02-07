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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultDeleteBallotTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultDeleteBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanCreator()
    {
        await CreateBallot(MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungElectionAdminClient.DeleteBallotAsync(NewValidRequest(req => req.BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await ErfassungCreatorClient.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorInCorrection()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await ErfassungCreatorClient.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungCreatorClient.DeleteBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBallotDeleted>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientBund.DeleteBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotDeleted>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.DeleteBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowInexistentBallotNumber()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.DeleteBallotAsync(NewValidRequest(x => x.BallotNumber = 99)),
            StatusCode.InvalidArgument,
            "only the last ballot can be deleted");
    }

    [Fact]
    public async Task TestShouldThrowWrongBallotNumber()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.DeleteBallotAsync(NewValidRequest(x => x.BallotNumber = 1)),
            StatusCode.InvalidArgument,
            "only the last ballot can be deleted");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.DeleteBallotAsync(new DeleteMajorityElectionResultBallotRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1,
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
            async () => await ErfassungElectionAdminClient.DeleteBallotAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.DeleteBallotAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBallotDeleted
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                BallotNumber = 1,
                EventInfo = GetMockedEventInfo(),
            });
        var ballot = await RunOnDb(db => db.MajorityElectionResultBallots
            .FirstOrDefaultAsync(x => x.BundleId == Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1)
                             && x.Number == 1));
        ballot.Should().BeNull();

        var bundle = await GetBundle();
        bundle.CountOfBallots.Should().Be(1);
        bundle.ElectionResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);

        await AssertHasPublishedEventProcessedMessage(MajorityElectionResultBallotDeleted.Descriptor, bundle.Id);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await CreateBallot();
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .DeleteBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await CreateBallot();
        await CreateBallot();
    }

    private DeleteMajorityElectionResultBallotRequest NewValidRequest(
        Action<DeleteMajorityElectionResultBallotRequest>? customizer = null)
    {
        var req = new DeleteMajorityElectionResultBallotRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            BallotNumber = LatestBallotNumber,
        };
        customizer?.Invoke(req);
        return req;
    }
}
