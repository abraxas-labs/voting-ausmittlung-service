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
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultSucceedBundleReviewTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultSucceedBundleReviewTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungCreatorClient.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungElectionAdminClient.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
            await ErfassungCreatorClient.SucceedBundleReviewAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleReviewSucceeded>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungElectionAdminClientBund.SucceedBundleReviewAsync(new SucceedMajorityElectionBundleReviewRequest
        {
            BundleIds = { MajorityElectionResultBundleMockedData.IdStGallenBundle1 },
        });
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.SucceedBundleReviewAsync(new SucceedMajorityElectionBundleReviewRequest
            {
                BundleIds = { MajorityElectionResultBundleMockedData.IdStGallenBundle1 },
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorSameUserAsBundleCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await ErfassungCreatorClient.SucceedBundleReviewAsync(new SucceedMajorityElectionBundleReviewRequest
            {
                BundleIds = { MajorityElectionResultBundleMockedData.IdStGallenBundle1 },
            }),
            StatusCode.PermissionDenied,
            "The creator of a bundle can't review it");
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungElectionAdminSameUserAsBundleCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SucceedBundleReviewAsync(new SucceedMajorityElectionBundleReviewRequest
            {
                BundleIds = { MajorityElectionResultBundleMockedData.IdStGallenBundle1 },
            }),
            StatusCode.PermissionDenied,
            "The creator of a bundle can't review it");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.SucceedBundleReviewAsync(
                new SucceedMajorityElectionBundleReviewRequest
                {
                    BundleIds = { MajorityElectionResultBundleMockedData.IdKircheBundle1 },
                }),
            StatusCode.PermissionDenied,
            "This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.SucceedBundleReviewAsync(NewValidRequest()),
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
        await RunBundleToState(state, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await AssertStatus(
            async () => await ErfassungCreatorClient.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await AssertStatus(
            async () => await ErfassungCreatorClient.SucceedBundleReviewAsync(
                NewValidRequest(x => x.BundleIds.Add(MajorityElectionResultBundleMockedData.IdStGallenBundle3))),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var resultId = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        var bundle1Id = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleReviewSucceeded
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            });
        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.Reviewed);
        bundle.ElectionResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);
        bundle.ElectionResult.AllBundlesReviewedOrDeleted.Should().BeFalse();
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);
        bundle.MatchSnapshot(x => x.ElectionResult.CountingCircleId);

        await AssertHasPublishedMessage<MajorityElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);
    }

    [Fact]
    public async Task TestProcessorUpdatesCandidateResults()
    {
        await ReplaceNullValuesWithZeroOnDetailedResults();

        await CreateBallot(MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await CreateBallot(Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle2));
        await ShouldHaveCandidateResults(false);

        await ErfassungCreatorClient.SucceedBundleReviewAsync(NewValidRequest());
        await RunEvents<MajorityElectionResultBundleReviewSucceeded>();

        await ShouldHaveCandidateResults(true);

        var candidateResults = await RunOnDb(
            db => db.MajorityElectionCandidateResults
                .Include(c => c.Candidate.Translations)
                .Where(c => c.ElectionResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))
                .OrderBy(c => c.CandidateId)
                .ToListAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(candidateResults.SelectMany(x => x.Candidate.Translations));
        candidateResults.MatchSnapshot("primary", x => x.Id);

        var secondaryCandidateResults = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidateResults
                .Include(c => c.Candidate.Translations)
                .Where(c => c.ElectionResult.PrimaryResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))
                .OrderBy(c => c.CandidateId)
                .ToListAsync(),
            Languages.Italian);
        SetDynamicIdToDefaultValue(secondaryCandidateResults.SelectMany(x => x.Candidate.Translations));
        secondaryCandidateResults.MatchSnapshot("secondary", x => x.Id, x => x.ElectionResultId);

        var result = await GetElectionResult();
        result.TotalCandidateVoteCountInclIndividual.Should().Be(2);
        result.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);
        result.AllBundlesReviewedOrDeleted.Should().BeFalse();
        result.ConventionalCountOfDetailedEnteredBallots.Should().Be(2);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var bundleId = await CreateBundle(10, "another-user");
        await RunBundleToState(BallotBundleState.ReadyForReview, bundleId);
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .SucceedBundleReviewAsync(new SucceedMajorityElectionBundleReviewRequest
            {
                BundleIds = { bundleId.ToString() },
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private SucceedMajorityElectionBundleReviewRequest NewValidRequest(Action<SucceedMajorityElectionBundleReviewRequest>? customizer = null)
    {
        var r = new SucceedMajorityElectionBundleReviewRequest
        {
            BundleIds = { MajorityElectionResultBundleMockedData.IdStGallenBundle3 },
        };

        customizer?.Invoke(r);
        return r;
    }
}
