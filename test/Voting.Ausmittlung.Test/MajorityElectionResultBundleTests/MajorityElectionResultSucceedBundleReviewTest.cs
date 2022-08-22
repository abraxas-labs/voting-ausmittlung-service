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
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleReviewSucceeded>()
            .MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunBundleToState(BallotBundleState.ReadyForReview);
            await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleReviewSucceeded>();
        });
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
                new SucceedMajorityElectionBundleReviewRequest
                {
                    BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1,
                }),
            StatusCode.PermissionDenied,
            "Invalid counting circle, does not belong to this tenant");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest()),
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
        await RunBundleToState(state);
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.SucceedBundleReviewAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
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
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(1);
        bundle.MatchSnapshot(x => x.ElectionResult.CountingCircleId);

        await AssertHasPublishedMessage<MajorityElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);
    }

    [Fact]
    public async Task TestProcessorUpdatesCandidateResults()
    {
        await CreateBallot();
        await CreateBallot();
        await CreateBallot(MajorityElectionResultBundleMockedData.IdStGallenBundle2);
        await ShouldHaveCandidateResults(false);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleReviewSucceeded
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                EventInfo = GetMockedEventInfo(),
            });

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
        result.CountOfBundlesNotReviewedOrDeleted.Should().Be(1);
        result.AllBundlesReviewedOrDeleted.Should().BeFalse();
        result.ConventionalCountOfDetailedEnteredBallots.Should().Be(2);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .SucceedBundleReviewAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private SucceedMajorityElectionBundleReviewRequest NewValidRequest()
    {
        return new SucceedMajorityElectionBundleReviewRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
        };
    }
}
