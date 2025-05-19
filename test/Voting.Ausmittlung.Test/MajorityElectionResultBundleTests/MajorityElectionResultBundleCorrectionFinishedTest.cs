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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultBundleCorrectionFinishedTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultBundleCorrectionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await ErfassungElectionAdminClient.BundleCorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunBundleToState(BallotBundleState.InCorrection);
            await ErfassungElectionAdminClient.BundleCorrectionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleCorrectionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanCreator()
    {
        await RunBundleToState(BallotBundleState.InCorrection, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungElectionAdminClient
            .BundleCorrectionFinishedAsync(new MajorityElectionResultBundleCorrectionFinishedRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3,
            });
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await ErfassungCreatorClient.BundleCorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await BundleErfassungElectionAdminClientBund.BundleCorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.BundleCorrectionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherUserThanBundleCreator()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.BundleCorrectionFinishedAsync(new MajorityElectionResultBundleCorrectionFinishedRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3,
            }),
            StatusCode.PermissionDenied,
            "only election admins or the creator of a bundle can edit it");
    }

    [Fact]
    public async Task TestShouldHaveRandomSamples()
    {
        for (var i = 1; i <= 5; i++)
        {
            await CreateBallot();
        }

        await RunBundleToState(BallotBundleState.InCorrection);
        await ErfassungCreatorClient.BundleCorrectionFinishedAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCorrectionFinished>();
        eventData.SampleBallotNumbers.Count.Should().Be(3);
        eventData.SampleBallotNumbers.Min().Should().BeGreaterOrEqualTo(1);
        eventData.SampleBallotNumbers.Max().Should().BeLessOrEqualTo(LatestBallotNumber);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.BundleCorrectionFinishedAsync(new MajorityElectionResultBundleCorrectionFinishedRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.BundleCorrectionFinishedAsync(NewValidRequest()),
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
            async () => await ErfassungElectionAdminClient.BundleCorrectionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var resultId = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        var bundle1Id = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1);

        for (var i = 0; i < 5; i++)
        {
            await CreateBallot();
        }

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleCorrectionFinished
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                SampleBallotNumbers =
                {
                        1,
                        3,
                        5,
                },
                EventInfo = GetMockedEventInfo(),
            });
        var bundleResp = await ErfassungElectionAdminClient.GetBundleAsync(
            new GetMajorityElectionResultBundleRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            });
        bundleResp.Bundle.State.Should().HaveSameValueAs(BallotBundleState.ReadyForReview);
        bundleResp.Bundle.BallotNumbersToReview.SequenceEqual(new List<int> { 1, 3, 5 }).Should().BeTrue();

        // these results are only calculated when the bundle is reviewed
        await ShouldHaveCandidateResults(false);

        await AssertHasPublishedEventProcessedMessage(MajorityElectionResultBundleCorrectionFinished.Descriptor, bundle1Id);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var bundleId = await CreateBundle(10);
        await RunBundleToState(BallotBundleState.InCorrection, bundleId);
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .BundleCorrectionFinishedAsync(new MajorityElectionResultBundleCorrectionFinishedRequest
            {
                BundleId = bundleId.ToString(),
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private MajorityElectionResultBundleCorrectionFinishedRequest NewValidRequest()
    {
        return new MajorityElectionResultBundleCorrectionFinishedRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
        };
    }
}
