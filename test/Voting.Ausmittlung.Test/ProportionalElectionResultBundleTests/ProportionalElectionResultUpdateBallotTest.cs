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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultUpdateBallotTest : ProportionalElectionResultBundleBaseTest
{
    public ProportionalElectionResultUpdateBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await BundleErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminOtherThanCreator()
    {
        await BundleErfassungElectionAdminClientSecondUser.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithAutomaticEmptyVoteCount()
    {
        await ErfassungElectionAdminClient.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotBundleSampleSize = 2,
                AutomaticEmptyVoteCounting = true,
                AutomaticBallotBundleNumberGeneration = true,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
        });
        await RunEvents<ProportionalElectionResultEntryDefined>();

        var bundleResponse = await BundleErfassungCreatorClient.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });
        await RunEvents<ProportionalElectionResultBundleCreated>();
        await CreateBallot(bundleResponse.BundleId);

        await BundleErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest(x =>
        {
            x.BundleId = bundleResponse.BundleId;
            x.EmptyVoteCount = null;
        }));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotUpdated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorOtherUserWhenBundleReadyForReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await BundleErfassungCreatorClientSecondUser.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBallotUpdated>();
        });
    }

    [Fact]
    public async Task TestShouldReturnEmptyBallot()
    {
        await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
        {
            x.Candidates.Clear();
            x.EmptyVoteCount = 3;
        }));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithUnchangedBallot()
    {
        var req = new UpdateProportionalElectionResultBallotRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            EmptyVoteCount = 0,
            BallotNumber = 1,
            Candidates =
            {
                new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 1,
                    CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                    OnList = true,
                },
                new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 2,
                    CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
                },
                new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 3,
                    CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                },
            },
        };
        await BundleErfassungCreatorClient.UpdateBallotAsync(req);
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherUser()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.UpdateBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "only election admins or the creator of a bundle can edit it");
    }

    [Fact]
    public async Task TestShouldThrowLockedContest()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowEmptyNoList()
    {
        var req = new UpdateProportionalElectionResultBallotRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle2,
            BallotNumber = 1,
            EmptyVoteCount = 3,
        };
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(req),
            StatusCode.InvalidArgument,
            "At least one candidate must be added.");
    }

    [Fact]
    public async Task TestShouldThrowTooManyCandidates()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
            {
                x.Candidates.Add(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 2,
                    CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
                });
                x.Candidates.Add(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 3,
                    CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                });
                x.Candidates.Add(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 3,
                    CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                });
            })),
            StatusCode.InvalidArgument,
            "too many candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowTriplicatedCandidate()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
            {
                x.Candidates.Add(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 2,
                    CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
                });
                x.Candidates.Add(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 3,
                    CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
                });
            })),
            StatusCode.InvalidArgument,
            "a candidate can be twice on the list at max");
    }

    [Fact]
    public async Task TestShouldThrowUnknownListCandidate()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
            {
                x.Candidates.Clear();
                x.Candidates.Add(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 1,
                    OnList = true,
                    CandidateId = ProportionalElectionMockedData.CandidateId3GossauProportionalElectionInContestStGallen,
                });
            })),
            StatusCode.InvalidArgument,
            "unknown list candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowUnknownCandidate()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
            {
                x.Candidates.Clear();
                x.Candidates.Add(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    Position = 1,
                    CandidateId = ProportionalElectionMockedData.CandidateIdKircheProportionalElectionInContestKirche,
                });
            })),
            StatusCode.InvalidArgument,
            "unknown candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherUserThanBundleCreatorInProcess()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.UpdateBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "only election admins or the creator of a bundle can edit it");
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorBundleCreatorReadyForReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "the creator of a bundle can't edit it while it is under review");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(new UpdateProportionalElectionResultBallotRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdUzwilBundle1,
                BallotNumber = 1,
                Candidates =
                {
                        new CreateUpdateProportionalElectionResultBallotCandidateRequest
                        {
                            Position = 1,
                            CandidateId = ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil,
                        },
                },
                EmptyVoteCount = 2,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowWrongBallotNumber()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.BallotNumber = 2)),
            StatusCode.InvalidArgument,
            "ballot number not found");
    }

    [Fact]
    public async Task TestShouldThrowNoEmptyVoteCountWithDisabledAutomaticCount()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = null)),
            StatusCode.InvalidArgument,
            "automatic empty vote counting is disabled");
    }

    [Fact]
    public async Task TestShouldThrowWrongEmptyVoteCountWithDisabledAutomaticCount()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = 3)),
            StatusCode.InvalidArgument,
            "wrong number of empty votes, expected: 2 provided: 3");
    }

    [Theory]
    [InlineData(BallotBundleState.Reviewed)]
    [InlineData(BallotBundleState.Deleted)]
    public async Task TestShouldThrowInWrongState(BallotBundleState state)
    {
        await RunBundleToState(state);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBallotUpdated
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                BallotNumber = 1,
                Candidates =
                {
                        new ProportionalElectionResultBallotUpdatedCandidateEventData
                        {
                            Position = 1,
                            OnList = true,
                            CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                        },
                        new ProportionalElectionResultBallotUpdatedCandidateEventData
                        {
                            Position = 2,
                            CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
                        },
                        new ProportionalElectionResultBallotUpdatedCandidateEventData
                        {
                            Position = 3,
                            OnList = false,
                            CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                        },
                },
                EmptyVoteCount = 0,
                EventInfo = GetMockedEventInfo(),
            });
        var ballot = await BundleErfassungCreatorClient.GetBallotAsync(
            new GetProportionalElectionResultBallotRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                BallotNumber = 1,
            });
        ballot.MatchSnapshot();

        var bundle = await GetBundle();
        bundle.CountOfBallots.Should().Be(1);
        bundle.ElectionResult.TotalCountOfBallots.Should().Be(0);
        bundle.ElectionResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(2);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .UpdateBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await CreateBallot();
    }

    private UpdateProportionalElectionResultBallotRequest NewValidRequest(
        Action<UpdateProportionalElectionResultBallotRequest>? customizer = null)
    {
        var req = new UpdateProportionalElectionResultBallotRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            BallotNumber = 1,
            Candidates =
                {
                    new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        Position = 1,
                        CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
                    },
                },
            EmptyVoteCount = 2,
        };
        customizer?.Invoke(req);
        return req;
    }
}
