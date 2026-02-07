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

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultCreateBallotTest : VoteResultBundleBaseTest
{
    public VoteResultCreateBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithRestartedBallotNumber()
    {
        var voteResultClient = CreateService<VoteResultService.VoteResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
        await voteResultClient.DefineEntryAsync(new DefineVoteResultEntryRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
            ResultEntryParams = new DefineVoteResultEntryParamsRequest
            {
                AutomaticBallotNumberGeneration = true,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
                BallotBundleSampleSizePercent = 10,
            },
        });
        var bundleResponse = await ErfassungElectionAdminClient.CreateBundleAsync(new CreateVoteResultBundleRequest
        {
            BundleNumber = 10,
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
        });
        await RunEvents<VoteResultBundleCreated>();

        await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest(x => x.BundleId = bundleResponse.BundleId));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotCreated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithManualBallotNumber()
    {
        var bundleResponse = await ErfassungElectionAdminClient.CreateBundleAsync(new CreateVoteResultBundleRequest
        {
            BundleNumber = 10,
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
        });
        await RunEvents<VoteResultBundleCreated>();

        await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest(x =>
        {
            x.BundleId = bundleResponse.BundleId;
            x.BallotNumber = 56234;
        }));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotCreated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBallotCreated>();
        });
    }

    [Fact]
    public async Task TestShouldReturnWhenInCorrection()
    {
        await RunBundleToState(BallotBundleState.InCorrection);
        await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotCreated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientStGallen.CreateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotCreated>().MatchSnapshot();
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
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.BundleId = VoteResultBundleMockedData.IdUzwilBundle1)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowDuplicatedQuestion()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x =>
                x.QuestionAnswers.Add(new CreateUpdateVoteResultBallotQuestionAnswerRequest
                {
                    QuestionNumber = 1,
                    Answer = SharedProto.BallotQuestionAnswer.No,
                }))),
            StatusCode.InvalidArgument,
            "duplicated questions provided");
    }

    [Fact]
    public async Task TestShouldThrowUnknownQuestion()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.QuestionAnswers[0] =
                new CreateUpdateVoteResultBallotQuestionAnswerRequest
                {
                    QuestionNumber = 3,
                    Answer = SharedProto.BallotQuestionAnswer.Yes,
                })),
            StatusCode.InvalidArgument,
            "unknown questions provided");
    }

    [Fact]
    public async Task TestShouldThrowDuplicatedTieBreakQuestion()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x =>
                x.TieBreakQuestionAnswers.Add(new CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest
                {
                    QuestionNumber = 1,
                    Answer = SharedProto.TieBreakQuestionAnswer.Q1,
                }))),
            StatusCode.InvalidArgument,
            "duplicated questions provided");
    }

    [Fact]
    public async Task TestShouldThrowUnknownTieBreakQuestion()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x => x.TieBreakQuestionAnswers[0] =
                new CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest
                {
                    QuestionNumber = 2,
                    Answer = SharedProto.TieBreakQuestionAnswer.Q1,
                })),
            StatusCode.InvalidArgument,
            "unknown questions provided");
    }

    [Fact]
    public async Task TestShouldThrowAllUnspecified()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest(x =>
            {
                foreach (var qa in x.QuestionAnswers)
                {
                    qa.Answer = SharedProto.BallotQuestionAnswer.Unspecified;
                }

                foreach (var qa in x.TieBreakQuestionAnswers)
                {
                    qa.Answer = SharedProto.TieBreakQuestionAnswer.Unspecified;
                }
            })),
            StatusCode.InvalidArgument,
            "At least one answer must be specified");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBallotAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestBallotNumberOverflowShouldThrow()
    {
        await ErfassungElectionAdminClient.CreateBallotAsync(NewValidRequest());

        var ev = EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotCreated>();
        ev.BallotNumber = int.MaxValue;

        await TestEventPublisher.Publish(GetNextEventNumber(), ev);
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
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBallotCreated
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                BallotNumber = 1,
                QuestionAnswers =
                {
                        new VoteResultBallotUpdatedQuestionAnswerEventData
                        {
                            QuestionNumber = 1,
                            Answer = SharedProto.BallotQuestionAnswer.Yes,
                        },
                        new VoteResultBallotUpdatedQuestionAnswerEventData
                        {
                            QuestionNumber = 2,
                            Answer = SharedProto.BallotQuestionAnswer.Yes,
                        },
                },
                TieBreakQuestionAnswers =
                {
                        new VoteResultBallotUpdatedTieBreakQuestionAnswerEventData
                        {
                            QuestionNumber = 1,
                            Answer = SharedProto.TieBreakQuestionAnswer.Q1,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        var ballot = await ErfassungCreatorClient.GetBallotAsync(
            new GetVoteResultBallotRequest
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                BallotNumber = 1,
            });
        ballot.MatchSnapshot();

        var bundle = await GetBundle();
        bundle.CountOfBallots.Should().Be(1);
        bundle.BallotResult.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);

        await AssertHasPublishedEventProcessedMessage(VoteResultBallotCreated.Descriptor, bundle.Id);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .CreateBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private CreateVoteResultBallotRequest NewValidRequest(
        Action<CreateVoteResultBallotRequest>? customizer = null)
    {
        var req = new CreateVoteResultBallotRequest
        {
            BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            QuestionAnswers =
                {
                    new CreateUpdateVoteResultBallotQuestionAnswerRequest
                    {
                        QuestionNumber = 1,
                        Answer = SharedProto.BallotQuestionAnswer.Yes,
                    },
                    new CreateUpdateVoteResultBallotQuestionAnswerRequest
                    {
                        QuestionNumber = 2,
                        Answer = SharedProto.BallotQuestionAnswer.Yes,
                    },
                },
            TieBreakQuestionAnswers =
                {
                    new CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest()
                    {
                        QuestionNumber = 1,
                        Answer = SharedProto.TieBreakQuestionAnswer.Q1,
                    },
                },
        };
        customizer?.Invoke(req);
        return req;
    }
}
