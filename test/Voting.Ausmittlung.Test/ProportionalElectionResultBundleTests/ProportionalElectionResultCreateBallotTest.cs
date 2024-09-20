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
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultCreateBallotTest : ProportionalElectionResultBundleBaseTest
{
    private readonly ProportionalElectionResultService.ProportionalElectionResultServiceClient _resultClient;

    public ProportionalElectionResultCreateBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
        _resultClient = CreateService<ProportionalElectionResultService.ProportionalElectionResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithContinuousBallotNumber()
    {
        await _resultClient.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotBundleSampleSize = 1,
                AutomaticEmptyVoteCounting = true,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
        });
        await RunEvents<ProportionalElectionResultEntryDefined>();

        var bundleResponse = await ErfassungCreatorClient.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            BundleNumber = 10,
            ListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });
        await RunEvents<ProportionalElectionResultBundleCreated>();

        await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest(x => x.BundleId = bundleResponse.BundleId));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnWithAutomaticEmptyVoteCount()
    {
        await _resultClient.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
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

        var bundleResponse = await ErfassungCreatorClient.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });
        await RunEvents<ProportionalElectionResultBundleCreated>();

        await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest(x =>
        {
            x.BundleId = bundleResponse.BundleId;
            x.EmptyVoteCount = null;
            x.Candidates.RemoveAt(1);
        }));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBallotCreated>();
        });
    }

    [Fact]
    public async Task TestShouldReturnWhenInCorrection()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnEmptyBallot()
    {
        await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x =>
        {
            x.Candidates.Clear();
            x.EmptyVoteCount = 3;
        }));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithUnchangedBallot()
    {
        var req = new CreateProportionalElectionResultBallotRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            EmptyVoteCount = 0,
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
        await ErfassungCreatorClient.CreateBallotAsync(req);
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientStGallen.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.CreateBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowLockedContest()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowEmptyNoList()
    {
        var req = new CreateProportionalElectionResultBallotRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle2,
            EmptyVoteCount = 3,
        };
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(req),
            StatusCode.InvalidArgument,
            "At least one candidate must be added.");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdUzwilBundle1,
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
    public async Task TestShouldThrowTooManyCandidates()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.Candidates.Add(new CreateUpdateProportionalElectionResultBallotCandidateRequest
            {
                Position = 4,
                CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
            }))),
            StatusCode.InvalidArgument,
            "too many candidates provided");
    }

    [Fact]
    public async Task TestShouldThrowTriplicatedCandidate()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.Candidates[0].CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen)),
            StatusCode.InvalidArgument,
            "a candidate can be twice on the list at max");
    }

    [Fact]
    public async Task TestShouldThrowUnknownListCandidate()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x =>
            {
                x.EmptyVoteCount = 2;
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
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x =>
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
    public async Task TestShouldThrowNoEmptyVoteCountWithDisabledAutomaticCount()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = null)),
            StatusCode.InvalidArgument,
            "automatic empty vote counting is disabled");
    }

    [Fact]
    public async Task TestShouldThrowWrongEmptyVoteCountWithDisabledAutomaticCount()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.EmptyVoteCount = 3)),
            StatusCode.InvalidArgument,
            "wrong number of empty votes, expected: 0 provided: 3");
    }

    [Fact]
    public async Task TestBallotNumberOverflowShouldThrow()
    {
        await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest());

        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBallotCreated>();
        ev.BallotNumber = int.MaxValue;

        await TestEventPublisher.Publish(ev);
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest()),
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
            async () => await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest()),
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
            new ProportionalElectionResultBallotCreated
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                BallotNumber = 1,
                Candidates =
                {
            new ProportionalElectionResultBallotUpdatedCandidateEventData
            {
                Position = 1,
                CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
            },
            new ProportionalElectionResultBallotUpdatedCandidateEventData
            {
                Position = 2,
                CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
            },
            new ProportionalElectionResultBallotUpdatedCandidateEventData
            {
                Position = 3,
                CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
            },
                },
                EmptyVoteCount = 0,
                EventInfo = GetMockedEventInfo(),
            });
        var ballot = await ErfassungCreatorClient.GetBallotAsync(
            new GetProportionalElectionResultBallotRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
                BallotNumber = 1,
            });
        ballot.MatchSnapshot();

        var bundle = await GetBundle();
        bundle.CountOfBallots.Should().Be(1);
        bundle.ElectionResult.TotalCountOfBallots.Should().Be(0);

        await AssertHasPublishedMessage<ProportionalElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);
    }

    [Fact]
    public async Task TestProcessorWithNonExistingBundleShouldReturn()
    {
        var bundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1);

        var bundleRepo = GetService<IDbRepository<DataContext, ProportionalElectionResultBundle>>();
        await bundleRepo.DeleteByKey(bundleId);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBallotCreated
            {
                BundleId = bundleId.ToString(),
                BallotNumber = 1,
                Candidates =
                {
            new ProportionalElectionResultBallotUpdatedCandidateEventData
            {
                Position = 1,
                CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
            },
            new ProportionalElectionResultBallotUpdatedCandidateEventData
            {
                Position = 2,
                CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
            },
            new ProportionalElectionResultBallotUpdatedCandidateEventData
            {
                Position = 3,
                CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
            },
                },
                EmptyVoteCount = 0,
                EventInfo = GetMockedEventInfo(),
            });

        await AssertStatus(
            async () => await ErfassungCreatorClient.GetBallotAsync(
                new GetProportionalElectionResultBallotRequest
                {
                    BundleId = bundleId.ToString(),
                    BallotNumber = 1,
                }),
            StatusCode.NotFound);

        (await bundleRepo.GetByKey(bundleId)).Should().BeNull();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .CreateBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private CreateProportionalElectionResultBallotRequest NewValidRequest(
    Action<CreateProportionalElectionResultBallotRequest>? customizer = null)
    {
        var req = new CreateProportionalElectionResultBallotRequest
        {
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
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
                        CandidateId = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
                        OnList = true,
                    },
                },
            EmptyVoteCount = 0,
        };
        customizer?.Invoke(req);
        return req;
    }
}
