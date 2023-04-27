// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultSucceedBundleReviewTest : ProportionalElectionResultBundleBaseTest
{
    public ProportionalElectionResultSucceedBundleReviewTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunBundleToState(BallotBundleState.ReadyForReview);
            await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBundleReviewSucceeded>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungElectionAdminClientStGallen.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
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
    public async Task TestShouldThrowLockedContest()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
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
                new SucceedProportionalElectionBundleReviewRequest
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
            async () => await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var resultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        var bundle1Id = Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1);

        await CreateBallot();
        await CreateBallot();

        await ShouldHaveCandidateResults(false);
        await ShouldHaveListResults(false);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleReviewSucceeded
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                EventInfo = GetMockedEventInfo(),
            });

        await ShouldHaveCandidateResults(true);
        await ShouldHaveListResults(true);

        var listResults = await RunOnDb(
            db => db.ProportionalElectionListResults
                .AsSplitQuery()
                .Include(c => c.CandidateResults)
                .ThenInclude(x => x.VoteSources)
                .Include(c => c.List.Translations)
                .Where(c => c.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen)
                .OrderBy(c => c.ListId)
                .ToListAsync(),
            Languages.French);

        foreach (var listResult in listResults)
        {
            foreach (var candidateResult in listResult.CandidateResults)
            {
                foreach (var voteSource in candidateResult.VoteSources)
                {
                    voteSource.CandidateResultId.Should().Be(candidateResult.Id);
                    voteSource.CandidateResult = null!;
                    voteSource.CandidateResultId = Guid.Empty;
                    voteSource.Id = Guid.Empty;
                }

                candidateResult.Id = Guid.Empty;
                candidateResult.ListResultId = Guid.Empty;
            }

            listResult.CandidateResults = listResult.CandidateResults.OrderBy(cr => cr.CandidateId).ToList();
            SetDynamicIdToDefaultValue(listResult.List.Translations);
        }

        listResults.MatchSnapshot(x => x.Id);

        var bundle = await GetBundle();
        bundle.State.Should().Be(BallotBundleState.Reviewed);
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(1);
        bundle.ElectionResult.TotalCountOfBallots.Should().Be(2);
        bundle.ElectionResult.TotalCountOfLists.Should().Be(2);
        bundle.ElectionResult.TotalCountOfModifiedLists.Should().Be(2);
        bundle.ElectionResult.TotalCountOfUnmodifiedLists.Should().Be(0);
        bundle.ElectionResult.TotalCountOfVoters.Should().Be(15_000);
        bundle.ElectionResult.TotalCountOfUnmodifiedLists.Should().Be(0);
        bundle.ElectionResult.TotalCountOfListsWithoutParty.Should().Be(0);
        bundle.ElectionResult.TotalCountOfListsWithParty.Should().Be(2);
        bundle.ElectionResult.TotalCountOfBlankRowsOnListsWithoutParty.Should().Be(0);

        await AssertHasPublishedMessage<ProportionalElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .SucceedBundleReviewAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private SucceedProportionalElectionBundleReviewRequest NewValidRequest()
    {
        return new SucceedProportionalElectionBundleReviewRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
        };
    }
}
