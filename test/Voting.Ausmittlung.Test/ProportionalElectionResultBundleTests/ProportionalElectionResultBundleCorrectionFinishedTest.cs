// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultBundleCorrectionFinishedTest : ProportionalElectionResultBundleBaseTest
{
    public ProportionalElectionResultBundleCorrectionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await BundleErfassungElectionAdminClient.BundleCorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanCreator()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await BundleErfassungElectionAdminClientSecondUser
            .BundleCorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await BundleErfassungCreatorClient.BundleCorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunBundleToState(BallotBundleState.InCorrection);
            await BundleErfassungCreatorClient.BundleCorrectionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBundleCorrectionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await BundleErfassungElectionAdminClientStGallen.BundleCorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.BundleCorrectionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherUserThanBundleCreator()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.BundleCorrectionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "only election admins or the creator of a bundle can edit it");
    }

    [Fact]
    public async Task TestShouldHaveRandomSamples()
    {
        var minBallotNr = 1;
        var maxBallotNr = 5;
        for (var i = minBallotNr; i <= maxBallotNr; i++)
        {
            await CreateBallot();
        }

        await RunBundleToState(BallotBundleState.InCorrection);

        await BundleErfassungCreatorClient.BundleCorrectionFinishedAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCorrectionFinished>();
        eventData.SampleBallotNumbers.Count.Should().Be(2);
        eventData.SampleBallotNumbers.Min().Should().BeGreaterOrEqualTo(minBallotNr);
        eventData.SampleBallotNumbers.Max().Should().BeLessOrEqualTo(LatestBallotNumber);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.BundleCorrectionFinishedAsync(new ProportionalElectionResultBundleCorrectionFinishedRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdUzwilBundle1,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowLockedContest()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.BundleCorrectionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Theory]
    [InlineData(BallotBundleState.InProcess)]
    [InlineData(BallotBundleState.ReadyForReview)]
    [InlineData(BallotBundleState.Reviewed)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await CreateBallot();
        await RunBundleToState(state);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.BundleCorrectionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var resultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        var bundle2Id = Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle2);
        for (var i = 0; i < 5; i++)
        {
            await CreateBallot();
        }

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleCorrectionFinished
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                SampleBallotNumbers =
                {
                        1,
                        3,
                        5,
                },
                EventInfo = GetMockedEventInfo(),
            });

        await CreateBallot(ProportionalElectionResultBundleMockedData.IdGossauBundle2);
        await CreateBallot(ProportionalElectionResultBundleMockedData.IdGossauBundle2);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleCorrectionFinished
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle2,
                EventInfo = GetMockedEventInfo(),
            });

        var bundleResp = await BundleErfassungElectionAdminClient.GetBundleAsync(
            new GetProportionalElectionResultBundleRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            });
        bundleResp.Bundle.State.Should().HaveSameValueAs(BallotBundleState.ReadyForReview);
        bundleResp.Bundle.BallotNumbersToReview.SequenceEqual(new List<int> { 1, 3, 5 }).Should().BeTrue();

        var result = await GetElectionResult();
        result.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);
        result.AllBundlesReviewedOrDeleted.Should().BeFalse();
        result.TotalCountOfBallots.Should().Be(0);
        result.TotalCountOfListsWithoutParty.Should().Be(0);
        result.TotalCountOfBlankRowsOnListsWithoutParty.Should().Be(0);

        // these results are only calculated when the bundle is reviewed
        await ShouldHaveCandidateResults(false);
        await ShouldHaveListResults(false);

        await AssertHasPublishedMessage<ProportionalElectionBundleChanged>(
            x => x.Id == bundle2Id && x.ElectionResultId == resultId);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .BundleCorrectionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private ProportionalElectionResultBundleCorrectionFinishedRequest NewValidRequest()
    {
        return new ProportionalElectionResultBundleCorrectionFinishedRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
        };
    }
}
