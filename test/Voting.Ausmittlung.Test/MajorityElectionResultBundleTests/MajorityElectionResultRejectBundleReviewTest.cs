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

public class MajorityElectionResultRejectBundleReviewTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultRejectBundleReviewTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungCreatorClient.RejectBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungElectionAdminClient.RejectBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
            await ErfassungCreatorClient.RejectBundleReviewAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleReviewRejected>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungElectionAdminClientBund.RejectBundleReviewAsync(new RejectMajorityElectionBundleReviewRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
        });
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.RejectBundleReviewAsync(new RejectMajorityElectionBundleReviewRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorSameUserAsBundleCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await ErfassungCreatorClient.RejectBundleReviewAsync(new RejectMajorityElectionBundleReviewRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            }),
            StatusCode.PermissionDenied,
            "The creator of a bundle can't review it");
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungElectionAdminSameUserAsBundleCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.RejectBundleReviewAsync(new RejectMajorityElectionBundleReviewRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            }),
            StatusCode.PermissionDenied,
            "The creator of a bundle can't review it");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.RejectBundleReviewAsync(
                new RejectMajorityElectionBundleReviewRequest
                {
                    BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1,
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
        await RunBundleToState(state, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await AssertStatus(
            async () => await ErfassungCreatorClient.RejectBundleReviewAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.RejectBundleReviewAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleReviewRejected
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            });
        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.InCorrection);
        bundle.ElectionResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);
        bundle.ElectionResult.AllBundlesReviewedOrDeleted.Should().BeFalse();
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(3);

        foreach (var log in bundle.Logs)
        {
            log.Id = Guid.Empty;
        }

        bundle.MatchSnapshot(x => x.ElectionResult.CountingCircleId);

        await AssertHasPublishedEventProcessedMessage(MajorityElectionResultBundleReviewRejected.Descriptor, Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1));
    }

    [Fact]
    public async Task TestProcessorUpdatesCandidateResults()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleReviewRejected
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
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
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .RejectBundleReviewAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private RejectMajorityElectionBundleReviewRequest NewValidRequest()
    {
        return new RejectMajorityElectionBundleReviewRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3,
        };
    }
}
