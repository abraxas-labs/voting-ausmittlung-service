// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultUpdateBallotTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultUpdateBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithContinuousBallotNumber()
    {
        var client = CreateService<MajorityElectionResultService.MajorityElectionResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
        await client.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotBundleSampleSize = 1,
                AutomaticEmptyVoteCounting = true,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        });
        await RunEvents<MajorityElectionResultEntryDefined>();

        var bundleResponse = await ErfassungCreatorClient.CreateBundleAsync(new CreateMajorityElectionResultBundleRequest
        {
            BundleNumber = 10,
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        await RunEvents<MajorityElectionResultBundleCreated>();
        await CreateBallot(Guid.Parse(bundleResponse.BundleId));

        await ErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest(x =>
        {
            x.BundleId = bundleResponse.BundleId;
            x.BallotNumber = LatestBallotNumber;
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnWithAutomaticEmptyVoteCount()
    {
        var client = CreateService<MajorityElectionResultService.MajorityElectionResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
        await client.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                AutomaticEmptyVoteCounting = true,
                BallotBundleSize = 1,
                BallotBundleSampleSize = 1,
                AutomaticBallotBundleNumberGeneration = true,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        });
        await RunEvents<MajorityElectionResultEntryDefined>();

        var bundleResponse = await ErfassungCreatorClient.CreateBundleAsync(new CreateMajorityElectionResultBundleRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        await RunEvents<MajorityElectionResultBundleCreated>();
        await CreateBallot(Guid.Parse(bundleResponse.BundleId));
        await ErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest(x =>
        {
            x.BallotNumber = 1;
            x.BundleId = bundleResponse.BundleId;
            x.EmptyVoteCount = null;
            x.SecondaryMajorityElectionResults[0].EmptyVoteCount = null;
            x.SecondaryMajorityElectionResults[0].SelectedCandidateIds.Clear();
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorOtherUserWhenBundleReadyForReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(req =>
        {
            req.BallotNumber = LatestBallotNumber;
            req.BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3;
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBallotUpdated>();
        });
    }

    [Fact]
    public async Task TestShouldReturnWithIndividualVotes()
    {
        await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
        {
            x.IndividualVoteCount = 1;
            x.SecondaryMajorityElectionResults[0].IndividualVoteCount = 1;
            x.SelectedCandidateIds.Clear();
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithInvalidVotes()
    {
        await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
        {
            x.InvalidVoteCount = 1;
            x.SecondaryMajorityElectionResults[0].InvalidVoteCount = 1;
            x.SecondaryMajorityElectionResults[0].EmptyVoteCount = 0;
            x.SelectedCandidateIds.Clear();
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithEmptyBallot()
    {
        await OverwriteMajorityElectionNumberOfMandates(Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund), 2);
        var req = NewValidRequest(x =>
        {
            x.EmptyVoteCount = 2;
            x.SelectedCandidateIds.Clear();
        });
        await ErfassungCreatorClient.UpdateBallotAsync(req);
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientBund.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.UpdateBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherUserThanBundleCreatorInProcess()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(req => req.BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3)),
            StatusCode.PermissionDenied,
            "only election admins or the creator of a bundle can edit it");
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorBundleCreatorReadyForReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "the creator of a bundle can't edit it while it is under review");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.BundleId = MajorityElectionResultBundleMockedData.IdKircheBundle1)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowUnknownBallotNumber()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.BallotNumber = 999)),
            StatusCode.InvalidArgument,
            "ballot number not found");
    }

    [Fact]
    public async Task TestShouldThrowTooManyInvalidVotes()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.InvalidVoteCount = 252311)),
            StatusCode.InvalidArgument,
            "too many candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowTooManyCandidates()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.SelectedCandidateIds.Add(MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund))),
            StatusCode.InvalidArgument,
            "too many candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowDuplicatedCandidate()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.SelectedCandidateIds.Add(MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund))),
            StatusCode.InvalidArgument,
            "duplicated candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowUnknownCandidate()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.SelectedCandidateIds[0] = MajorityElectionMockedData.CandidateIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument,
            "unknown candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowNoEmptyVoteCountWithDisabledAutomaticCount()
    {
        await OverwriteMajorityElectionNumberOfMandates(Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund), 2);
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = null)),
            StatusCode.InvalidArgument,
            "automatic empty vote counting is disabled");
    }

    [Fact]
    public async Task TestShouldThrowWrongEmptyVoteCountWithDisabledAutomaticCount()
    {
        await OverwriteMajorityElectionNumberOfMandates(Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund), 2);
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = 3)),
            StatusCode.InvalidArgument,
            "wrong number of empty votes, expected: 1 provided: 3");
    }

    [Fact]
    public async Task TestShouldThrowWrongEmptyVoteCountOnSecondaryWithDisabledAutomaticCount()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.SecondaryMajorityElectionResults[0].EmptyVoteCount = 3)),
            StatusCode.InvalidArgument,
            "wrong number of empty votes, expected: 1 provided: 3");
    }

    [Fact]
    public async Task TestShouldWithNonNullIndividualVotesWhenDisabled()
    {
        await ModifyDbEntities<MajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
            x => x.IndividualCandidatesDisabled = true);

        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
            {
                x.IndividualVoteCount = 1;
                x.SelectedCandidateIds.Clear();
            })),
            StatusCode.InvalidArgument,
            "Individual vote count is disabled on election");
    }

    [Fact]
    public async Task TestShouldWithNonNullSecondaryIndividualVotesWhenDisabled()
    {
        await ModifyDbEntities<SecondaryMajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund),
            x => x.IndividualCandidatesDisabled = true);

        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Individual vote count is disabled on election");
    }

    [Theory]
    [InlineData(BallotBundleState.Reviewed)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await RunBundleToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBallotUpdated
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
                            InvalidVoteCount = 1,
                            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                            SelectedCandidateIds =
                            {
                                MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                            },
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        var ballot = await ErfassungCreatorClient.GetBallotAsync(
            new GetMajorityElectionResultBallotRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
                BallotNumber = 1,
            });
        ballot.MatchSnapshot();

        var bundle = await GetBundle();
        bundle.CountOfBallots.Should().Be(1);
        bundle.ElectionResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(3);
    }

    [Fact]
    public async Task TestShouldThrowEmptyVoteCountProvideWithSingleMandate()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = 0)),
            StatusCode.InvalidArgument,
            "empty vote count provided with single mandate");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .UpdateBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await CreateBallot();
    }

    private UpdateMajorityElectionResultBallotRequest NewValidRequest(
        Action<UpdateMajorityElectionResultBallotRequest>? customizer = null)
    {
        var req = new UpdateMajorityElectionResultBallotRequest
        {
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            BallotNumber = 1,
            IndividualVoteCount = 0,
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

    private async Task OverwriteMajorityElectionNumberOfMandates(Guid majorityElectionId, int numberOfMandates)
    {
        await ModifyDbEntities<MajorityElection>(
            me => me.Id == majorityElectionId,
            me => me.NumberOfMandates = numberOfMandates);
    }
}
