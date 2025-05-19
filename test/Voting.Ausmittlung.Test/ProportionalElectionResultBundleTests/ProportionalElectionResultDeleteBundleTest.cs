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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultDeleteBundleTest : ProportionalElectionResultBundleBaseTest
{
    private int _bundleNumber = 10;

    public ProportionalElectionResultDeleteBundleTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await CreateBallot();
        await ErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanBundleCreator()
    {
        await CreateBallot();
        await ErfassungElectionAdminClient.DeleteBundleAsync(new DeleteProportionalElectionResultBundleRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle3,
        });
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CreateBallot();
            await ErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest());
            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBundleDeleted>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBundleNumberFreed>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await CreateBallot();
        await BundleErfassungElectionAdminClientStGallen.DeleteBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await CreateBallot();
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.DeleteBundleAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowLockedContest()
    {
        await CreateBallot();
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DeleteBundleAsync(new DeleteProportionalElectionResultBundleRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle3,
            }),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Theory]
    [InlineData(BallotBundleState.InProcess)]
    [InlineData(BallotBundleState.InCorrection)]
    [InlineData(BallotBundleState.ReadyForReview)]
    [InlineData(BallotBundleState.Reviewed)]
    public async Task TestShouldReturnForStates(BallotBundleState state)
    {
        await RunBundleToState(state);
        await ErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherThanBundleCreator()
    {
        await CreateBallot();
        await SetBundleDeleted();
        await AssertStatus(
            async () => await ErfassungCreatorClient.DeleteBundleAsync(new DeleteProportionalElectionResultBundleRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle3,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DeleteBundleAsync(new DeleteProportionalElectionResultBundleRequest
            {
                BundleId = "a8c45178-eae2-4741-8a08-444704162ffd",
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.DeleteBundleAsync(new DeleteProportionalElectionResultBundleRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdUzwilBundle1,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAlreadyDeleted()
    {
        await SetBundleDeleted();
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "bundle is already deleted");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleDeleted
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.Deleted);
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);
        bundle.ElectionResult.TotalCountOfBallots.Should().Be(0);

        foreach (var log in bundle.Logs)
        {
            log.Id = Guid.Empty;
        }

        bundle.MatchSnapshot(x => x.ElectionResult.CountingCircleId);

        await AssertHasPublishedEventProcessedMessage(ProportionalElectionResultBundleDeleted.Descriptor, bundle.Id);
    }

    [Fact]
    public async Task TestProcessorDoesNotUpdateCandidateAndListResultsIfNotReviewedYes()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await ShouldHaveCandidateResults(false);
        await ShouldHaveListResults(false);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleDeleted
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        await ShouldHaveCandidateResults(false);
        await ShouldHaveListResults(false);
    }

    [Fact]
    public async Task TestProcessorUpdatesCandidateAndListResults()
    {
        await RunBundleToState(BallotBundleState.Reviewed);
        await ShouldHaveCandidateResults(true);
        await ShouldHaveListResults(true);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleDeleted
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        await ShouldHaveCandidateResults(false);
        await ShouldHaveListResults(false);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var response = await ErfassungCreatorClient.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            BundleNumber = _bundleNumber++,
        });
        await RunEvents<ProportionalElectionResultBundleCreated>();

        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .DeleteBundleAsync(new DeleteProportionalElectionResultBundleRequest
            {
                BundleId = response.BundleId,
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private DeleteProportionalElectionResultBundleRequest NewValidRequest()
    {
        return new DeleteProportionalElectionResultBundleRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
        };
    }
}
