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
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultDeleteBundleTest : MajorityElectionResultBundleBaseTest
{
    private bool _initializedAuthorizationTest;

    public MajorityElectionResultDeleteBundleTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await CreateBallot();
        await ErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanBundleCreator()
    {
        await CreateBallot(MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungElectionAdminClient.DeleteBundleAsync(new DeleteMajorityElectionResultBundleRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3,
        });
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await CreateBallot(MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
            await ErfassungElectionAdminClient.DeleteBundleAsync(new DeleteMajorityElectionResultBundleRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3,
            });
            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleDeleted>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleNumberFreed>(),
            };
        });
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
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await CreateBallot();
        await BundleErfassungElectionAdminClientBund.DeleteBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleDeleted>().MatchSnapshot("deleted");
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleNumberFreed>().MatchSnapshot("freed");
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.DeleteBundleAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherThanBundleCreator()
    {
        await CreateBallot();
        await SetBundleDeleted();
        await AssertStatus(
            async () => await ErfassungCreatorClient.DeleteBundleAsync(new DeleteMajorityElectionResultBundleRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DeleteBundleAsync(new DeleteMajorityElectionResultBundleRequest
            {
                BundleId = "a8c45178-eae2-4741-8a08-444704162ffd",
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.DeleteBundleAsync(new DeleteMajorityElectionResultBundleRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1,
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
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DeleteBundleAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var resultId = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        var bundle1Id = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleDeleted
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.Deleted);
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);
        bundle.ElectionResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);

        await AssertHasPublishedMessage<MajorityElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);
    }

    [Fact]
    public async Task TestProcessorUpdatesCandidateResultsIfNotReviewedYet()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await ShouldHaveCandidateResults(false);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleDeleted
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        await ShouldHaveCandidateResults(false);
    }

    [Fact]
    public async Task TestProcessorUpdatesCandidateResults()
    {
        await ReplaceNullValuesWithZeroOnDetailedResults();

        await RunBundleToState(BallotBundleState.Reviewed);
        await ShouldHaveCandidateResults(true);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleDeleted
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                EventInfo = GetMockedEventInfo(),
            });
        await ShouldHaveCandidateResults(false);
    }

    [Fact]
    public async Task TestProcessorDoesntUpdateCandidateResultsIfInProcess()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleDeleted
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                EventInfo = GetMockedEventInfo(),
            });

        var hasNotZeroCandidateResults = await RunOnDb(db => db.MajorityElectionCandidateResults
            .AnyAsync(c =>
                c.ElectionResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund) &&
                c.VoteCount != 0));
        hasNotZeroCandidateResults.Should().BeFalse();

        var hasNotZeroSecondaryCandidateResults = await RunOnDb(db => db.SecondaryMajorityElectionCandidateResults
            .AnyAsync(c =>
                c.ElectionResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund) &&
                c.VoteCount != 0));
        hasNotZeroSecondaryCandidateResults.Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (!_initializedAuthorizationTest)
        {
            _initializedAuthorizationTest = true;
            await DefineResultEntry();
        }

        var bundleNumber = await GenerateBundleNumber();
        var bundleId = await CreateBundle(bundleNumber);

        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .DeleteBundleAsync(new DeleteMajorityElectionResultBundleRequest
            {
                BundleId = bundleId.ToString(),
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private DeleteMajorityElectionResultBundleRequest NewValidRequest()
    {
        return new DeleteMajorityElectionResultBundleRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
        };
    }
}
