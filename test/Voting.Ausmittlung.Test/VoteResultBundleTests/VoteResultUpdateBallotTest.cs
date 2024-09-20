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

public class VoteResultUpdateBallotTest : VoteResultBundleBaseTest
{
    public VoteResultUpdateBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithRestartedBallotNumber()
    {
        var bundleResponse = await ErfassungElectionAdminClient.CreateBundleAsync(new CreateVoteResultBundleRequest
        {
            BundleNumber = 10,
            BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
        });
        await RunEvents<VoteResultBundleCreated>();
        await CreateBallot(Guid.Parse(bundleResponse.BundleId));

        await ErfassungElectionAdminClient.UpdateBallotAsync(NewValidRequest(x =>
        {
            x.BundleId = bundleResponse.BundleId;
            x.BallotNumber = LatestBallotNumber;
        }));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotUpdated>().MatchSnapshot(x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBallotUpdated>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorOtherUserWhenBundleReadyForReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, VoteResultBundleMockedData.GossauBundle3.Id);
        await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(req => req.BundleId = VoteResultBundleMockedData.IdGossauBundle3));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientStGallen.UpdateBallotAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBallotUpdated>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.UpdateBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungCreatorOtherUserThanBundleCreatorInProcess()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(req => req.BundleId = VoteResultBundleMockedData.IdGossauBundle3)),
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
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x => x.BundleId = VoteResultBundleMockedData.IdUzwilBundle1)),
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
    public async Task TestShouldThrowDuplicatedQuestion()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
                x.QuestionAnswers.Add(new CreateUpdateVoteResultBallotQuestionAnswerRequest
                {
                    Answer = SharedProto.BallotQuestionAnswer.Yes,
                    QuestionNumber = 1,
                }))),
            StatusCode.InvalidArgument,
            "duplicated questions provided");
    }

    [Fact]
    public async Task TestShouldThrowUnknownQuestion()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
                x.QuestionAnswers[0] = new CreateUpdateVoteResultBallotQuestionAnswerRequest
                {
                    Answer = SharedProto.BallotQuestionAnswer.Yes,
                    QuestionNumber = 3,
                })),
            StatusCode.InvalidArgument,
            "unknown questions provided");
    }

    [Fact]
    public async Task TestShouldThrowDuplicatedTieBreakQuestion()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
                x.TieBreakQuestionAnswers.Add(new CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest
                {
                    Answer = SharedProto.TieBreakQuestionAnswer.Q1,
                    QuestionNumber = 1,
                }))),
            StatusCode.InvalidArgument,
            "duplicated questions provided");
    }

    [Fact]
    public async Task TestShouldThrowUnknownTieBreakQuestion()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
                x.TieBreakQuestionAnswers[0] = new CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest
                {
                    Answer = SharedProto.TieBreakQuestionAnswer.Q1,
                    QuestionNumber = 2,
                })),
            StatusCode.InvalidArgument,
            "unknown questions provided");
    }

    [Fact]
    public async Task TestShouldThrowAllUnspecified()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.UpdateBallotAsync(NewValidRequest(x =>
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
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
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
            new VoteResultBallotUpdated
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                BallotNumber = 1,
                QuestionAnswers =
                {
                        new VoteResultBallotUpdatedQuestionAnswerEventData
                        {
                            QuestionNumber = 1,
                            Answer = SharedProto.BallotQuestionAnswer.No,
                        },
                        new VoteResultBallotUpdatedQuestionAnswerEventData
                        {
                            QuestionNumber = 2,
                            Answer = SharedProto.BallotQuestionAnswer.Unspecified,
                        },
                },
                TieBreakQuestionAnswers =
                {
                        new VoteResultBallotUpdatedTieBreakQuestionAnswerEventData
                        {
                            QuestionNumber = 1,
                            Answer = SharedProto.TieBreakQuestionAnswer.Unspecified,
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
        bundle.BallotResult.CountOfBundlesNotReviewedOrDeleted.Should().Be(3);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
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

    private UpdateVoteResultBallotRequest NewValidRequest(
        Action<UpdateVoteResultBallotRequest>? customizer = null)
    {
        var req = new UpdateVoteResultBallotRequest
        {
            BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            BallotNumber = 1,
            QuestionAnswers =
                {
                    new CreateUpdateVoteResultBallotQuestionAnswerRequest
                    {
                        QuestionNumber = 1,
                        Answer = SharedProto.BallotQuestionAnswer.No,
                    },
                    new CreateUpdateVoteResultBallotQuestionAnswerRequest
                    {
                        QuestionNumber = 2,
                        Answer = SharedProto.BallotQuestionAnswer.Unspecified,
                    },
                },
            TieBreakQuestionAnswers =
                {
                    new CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest()
                    {
                        QuestionNumber = 1,
                        Answer = SharedProto.TieBreakQuestionAnswer.Unspecified,
                    },
                },
        };
        customizer?.Invoke(req);
        return req;
    }
}
