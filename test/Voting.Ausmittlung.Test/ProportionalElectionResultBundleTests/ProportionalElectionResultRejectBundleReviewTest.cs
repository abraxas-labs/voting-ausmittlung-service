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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultRejectBundleReviewTest : ProportionalElectionResultBundleBaseTest
{
    public ProportionalElectionResultRejectBundleReviewTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.RejectBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungElectionAdminClientSecondUser.RejectBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunBundleToState(BallotBundleState.ReadyForReview);
            await BundleErfassungCreatorClientSecondUser.RejectBundleReviewAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBundleReviewRejected>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungElectionAdminClientStGallen.RejectBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleReviewRejected>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
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
    public async Task TestShouldThrowLockedContest()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientSecondUser.RejectBundleReviewAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
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
                new RejectProportionalElectionBundleReviewRequest
                {
                    BundleId = ProportionalElectionResultBundleMockedData.IdUzwilBundle1,
                }),
            StatusCode.PermissionDenied,
            "This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
    }

    [Theory]
    [InlineData(BallotBundleState.InProcess)]
    [InlineData(BallotBundleState.InCorrection)]
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
    public async Task TestProcessor()
    {
        var resultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        var bundle1Id = Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleReviewRejected
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            });
        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.InCorrection);
        bundle.ElectionResult.TotalCountOfBallots.Should().Be(0);
        bundle.ElectionResult.AllBundlesReviewedOrDeleted.Should().BeFalse();
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);
        bundle.MatchSnapshot(x => x.ElectionResult.CountingCircleId);

        await AssertHasPublishedMessage<ProportionalElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);
    }

    [Fact]
    public async Task TestProcessorUpdatesListResults()
    {
        await CreateBallot(ProportionalElectionResultBundleMockedData.IdGossauBundle2);
        await CreateBallot(ProportionalElectionResultBundleMockedData.IdGossauBundle2);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleSubmissionFinished
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle2,
                EventInfo = GetMockedEventInfo(),
            });

        await RunBundleToState(BallotBundleState.ReadyForReview);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleReviewRejected
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            },
            new ProportionalElectionResultBundleReviewRejected
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle2,
            });

        var hasNonZeroListResults = await RunOnDb(db => db.ProportionalElectionListResults
            .AnyAsync(c =>
                c.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen &&
                (c.ConventionalSubTotal.ModifiedListsCount != 0 || c.ConventionalSubTotal.ModifiedListVotesCount != 0 || c.ConventionalSubTotal.ModifiedListBlankRowsCount != 0)));
        hasNonZeroListResults.Should().BeFalse();

        var result = await GetElectionResult();
        result.TotalCountOfListsWithoutParty.Should().Be(0);
        result.TotalCountOfBlankRowsOnListsWithoutParty.Should().Be(0);
    }

    [Fact]
    public async Task TestProcessorUpdatesCandidateResults()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleReviewRejected
            {
                EventInfo = GetMockedEventInfo(),
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            });

        var hasNonZeroCandidateResults = await RunOnDb(db => db.ProportionalElectionCandidateResults
            .AnyAsync(c =>
                c.ListResult.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen &&
                (c.ConventionalSubTotal.ModifiedListVotesCount != 0 || c.ConventionalSubTotal.CountOfVotesOnOtherLists != 0 || c.ConventionalSubTotal.CountOfVotesFromAccumulations != 0)));
        hasNonZeroCandidateResults.Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .RejectBundleReviewAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private RejectProportionalElectionBundleReviewRequest NewValidRequest()
    {
        return new RejectProportionalElectionBundleReviewRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
        };
    }
}
