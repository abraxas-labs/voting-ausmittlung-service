// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
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
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultCreateBallotTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultCreateBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await BundleErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithContinuousBallotNumber()
    {
        await ErfassungElectionAdminClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotBundleSampleSize = 2,
                AutomaticEmptyVoteCounting = true,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        });
        await RunEvents<MajorityElectionResultEntryDefined>();

        var bundleResponse = await BundleErfassungCreatorClient.CreateBundleAsync(new CreateMajorityElectionResultBundleRequest
        {
            BundleNumber = 10,
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        await RunEvents<MajorityElectionResultBundleCreated>();

        await BundleErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest(x => x.BundleId = bundleResponse.BundleId));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnWithAutomaticEmptyVoteCount()
    {
        await ErfassungElectionAdminClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                AutomaticEmptyVoteCounting = true,
                BallotBundleSize = 1,
                BallotBundleSampleSize = 1,
                AutomaticBallotBundleNumberGeneration = true,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        });
        await RunEvents<MajorityElectionResultEntryDefined>();

        var bundleResponse = await BundleErfassungCreatorClient.CreateBundleAsync(new CreateMajorityElectionResultBundleRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        await RunEvents<MajorityElectionResultBundleCreated>();
        await BundleErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest(x =>
        {
            x.BundleId = bundleResponse.BundleId;
            x.EmptyVoteCount = null;
            x.SecondaryMajorityElectionResults[0].EmptyVoteCount = null;
            x.SecondaryMajorityElectionResults[0].SelectedCandidateIds.Clear();
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBallotCreated>();
        });
    }

    [Fact]
    public async Task TestShouldReturnWhenInCorrection()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithIndividualVotes()
    {
        await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x =>
        {
            x.IndividualVoteCount = 1;
            x.SelectedCandidateIds.Clear();
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithInvalidVotes()
    {
        await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x =>
        {
            x.InvalidVoteCount = 1;
            x.SelectedCandidateIds.Clear();
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnEmpty()
    {
        var req = NewValidRequest(x =>
        {
            x.EmptyVoteCount = 1;
            x.SelectedCandidateIds.Clear();
        });
        await BundleErfassungCreatorClient.CreateBallotAsync(req);
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientBund.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.CreateBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowTooManyInvalidVotes()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.InvalidVoteCount = 132549)),
            StatusCode.InvalidArgument,
            "too many candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowTooManyCandidates()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.SelectedCandidateIds.Add(MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund))),
            StatusCode.InvalidArgument,
            "too many candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowDuplicatedCandidate()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.SelectedCandidateIds.Add(MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund))),
            StatusCode.InvalidArgument,
            "duplicated candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowUnknownCandidate()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.SelectedCandidateIds[0] = MajorityElectionMockedData.CandidateIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument,
            "unknown candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowNoEmptyVoteCountWithDisabledAutomaticCount()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = null)),
            StatusCode.InvalidArgument,
            "automatic empty vote counting is disabled");
    }

    [Fact]
    public async Task TestShouldThrowWrongEmptyVoteCountWithDisabledAutomaticCount()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = 3)),
            StatusCode.InvalidArgument,
            "wrong number of empty votes, expected: 0 provided: 3");
    }

    [Fact]
    public async Task TestShouldThrowWrongEmptyVoteCountOnSecondaryWithDisabledAutomaticCount()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.SecondaryMajorityElectionResults[0].EmptyVoteCount = 3)),
            StatusCode.InvalidArgument,
            "wrong number of empty votes, expected: 1 provided: 3");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestBallotNumberOverflowShouldThrow()
    {
        await BundleErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest());

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotCreated>();
        ev.BallotNumber = int.MaxValue;

        await TestEventPublisher.Publish(ev);
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.CreateBallotAsync(NewValidRequest()),
            StatusCode.Internal,
            nameof(OverflowException));
    }

    [Theory]
    [InlineData(BallotBundleState.ReadyForReview)]
    [InlineData(BallotBundleState.Reviewed)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await RunBundleToState(state);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest()),
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
            new MajorityElectionResultBallotCreated
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                BallotNumber = 1,
                IndividualVoteCount = 0,
                EmptyVoteCount = 0,
                SelectedCandidateIds =
                {
                        MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                },
                SecondaryMajorityElectionResults =
                {
                        new SecondaryMajorityElectionResultBallotEventData
                        {
                            EmptyVoteCount = 1,
                            IndividualVoteCount = 1,
                            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                            SelectedCandidateIds =
                            {
                                MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                            },
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        var ballot = await BundleErfassungCreatorClient.GetBallotAsync(
            new GetMajorityElectionResultBallotRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                BallotNumber = 1,
            });
        ballot.MatchSnapshot();

        var bundle = await GetBundle();
        bundle.CountOfBallots.Should().Be(1);
        bundle.ElectionResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);

        await AssertHasPublishedMessage<MajorityElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .CreateBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private CreateMajorityElectionResultBallotRequest NewValidRequest(
        Action<CreateMajorityElectionResultBallotRequest>? customizer = null)
    {
        var req = new CreateMajorityElectionResultBallotRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            IndividualVoteCount = 0,
            EmptyVoteCount = 0,
            SelectedCandidateIds =
                {
                    MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                },
            SecondaryMajorityElectionResults =
                {
                    new CreateUpdateSecondaryMajorityElectionResultBallotRequest
                    {
                        EmptyVoteCount = 1,
                        IndividualVoteCount = 1,
                        SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                        SelectedCandidateIds =
                        {
                            MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                        },
                    },
                },
        };
        customizer?.Invoke(req);
        return req;
    }
}
