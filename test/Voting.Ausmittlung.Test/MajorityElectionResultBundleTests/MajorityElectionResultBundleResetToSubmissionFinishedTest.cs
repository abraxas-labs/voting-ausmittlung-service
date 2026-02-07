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

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultBundleResetToSubmissionFinishedTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultBundleResetToSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunBundleToState(BallotBundleState.Reviewed);
        await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleResetToSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunBundleToState(BallotBundleState.Reviewed);
            await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleResetToSubmissionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunBundleToState(BallotBundleState.Reviewed);
        await BundleErfassungElectionAdminClientBund.BundleResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleResetToSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await RunBundleToState(BallotBundleState.Reviewed);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.BundleResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunBundleToState(BallotBundleState.Reviewed);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(new MajorityElectionResultBundleResetToSubmissionFinishedRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1,
            }),
            StatusCode.PermissionDenied);
    }

    [Theory]
    [InlineData(BallotBundleState.InCorrection)]
    [InlineData(BallotBundleState.ReadyForReview)]
    [InlineData(BallotBundleState.InProcess)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await RunBundleToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.BundleResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await CreateBallot();
        await CreateBallot();
        await RunBundleToState(BallotBundleState.Reviewed);
        await RunAllEvents();

        var bundleBefore = await GetBundle();
        bundleBefore.State.Should().Be(BallotBundleState.Reviewed);
        bundleBefore.ElectionResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(3);
        bundleBefore.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleResetToSubmissionFinished
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                ElectionResultId = bundleBefore.ElectionResultId.ToString(),
                SampleBallotNumbers =
                {
                    1,
                },
                EventInfo = GetMockedEventInfo(),
            });

        var bundleAfter = await GetBundle();
        bundleAfter.State.Should().Be(BallotBundleState.ReadyForReview);
        bundleAfter.ElectionResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);
        bundleAfter.ElectionResult.AllBundlesReviewedOrDeleted.Should().BeFalse();
        bundleAfter.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(3);

        foreach (var log in bundleAfter.Logs)
        {
            log.Id = Guid.Empty;
        }

        bundleAfter.MatchSnapshot(x => x.ElectionResult.CountingCircleId);
        await AssertHasPublishedEventProcessedMessage(MajorityElectionResultBundleResetToSubmissionFinished.Descriptor, bundleAfter.Id);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var bundleId = await CreateBundle(10);
        await RunBundleToState(BallotBundleState.Reviewed, bundleId);
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .BundleResetToSubmissionFinishedAsync(new MajorityElectionResultBundleResetToSubmissionFinishedRequest
            {
                BundleId = bundleId.ToString(),
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private MajorityElectionResultBundleResetToSubmissionFinishedRequest NewValidRequest()
    {
        return new MajorityElectionResultBundleResetToSubmissionFinishedRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
        };
    }
}
