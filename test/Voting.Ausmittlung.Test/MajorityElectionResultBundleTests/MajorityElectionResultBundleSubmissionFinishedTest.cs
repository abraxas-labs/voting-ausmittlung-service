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

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultBundleSubmissionFinishedTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultBundleSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await CreateBallot();
        await ErfassungElectionAdminClient.BundleSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await CreateBallot();
            await ErfassungElectionAdminClient.BundleSubmissionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleSubmissionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanCreator()
    {
        await CreateBallot(MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungElectionAdminClient
            .BundleSubmissionFinishedAsync(new MajorityElectionResultBundleSubmissionFinishedRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3,
            });
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await CreateBallot();
        await ErfassungCreatorClient.BundleSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await CreateBallot();
        await BundleErfassungElectionAdminClientBund.BundleSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.BundleSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherUserThanBundleCreator()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.BundleSubmissionFinishedAsync(new MajorityElectionResultBundleSubmissionFinishedRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3,
            }),
            StatusCode.PermissionDenied,
            "only election admins or the creator of a bundle can edit it");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.BundleSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldHaveRandomSamples()
    {
        var minBallotNr = 1;
        var maxBallotNr = 10;
        for (var i = minBallotNr; i <= maxBallotNr; i++)
        {
            await CreateBallot();
        }

        await ErfassungCreatorClient.BundleSubmissionFinishedAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleSubmissionFinished>();
        eventData.SampleBallotNumbers.Count.Should().Be(2);
        eventData.SampleBallotNumbers.Min().Should().BeGreaterOrEqualTo(minBallotNr);
        eventData.SampleBallotNumbers.Max().Should().BeLessOrEqualTo(maxBallotNr);
    }

    [Fact]
    public async Task TestShouldThrowWithoutBallot()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.BundleSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "at least one ballot is required to close this bundle");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.BundleSubmissionFinishedAsync(new MajorityElectionResultBundleSubmissionFinishedRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1,
            }),
            StatusCode.PermissionDenied);
    }

    [Theory]
    [InlineData(BallotBundleState.InCorrection)]
    [InlineData(BallotBundleState.ReadyForReview)]
    [InlineData(BallotBundleState.Reviewed)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await CreateBallot();
        await RunBundleToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.BundleSubmissionFinishedAsync(NewValidRequest()),
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
            new MajorityElectionResultBundleSubmissionFinished
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

        await AssertHasPublishedMessage<MajorityElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var bundleId = await CreateBundle(10);
        await CreateBallot(bundleId);
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .BundleSubmissionFinishedAsync(new MajorityElectionResultBundleSubmissionFinishedRequest
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

    private MajorityElectionResultBundleSubmissionFinishedRequest NewValidRequest()
    {
        return new MajorityElectionResultBundleSubmissionFinishedRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
        };
    }
}
