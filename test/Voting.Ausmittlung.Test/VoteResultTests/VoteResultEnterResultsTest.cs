// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultEnterResultsTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultEnterResultsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.EnterResultsAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultEntered>().MatchSnapshot();

        await RunEvents<VoteResultEntered>(false);

        await ErfassungElectionAdminClient.EnterResultsAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungElectionAdminClient.EnterResultsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultEntered>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await StGallenErfassungElectionAdminClient.EnterResultsAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultEntered>().MatchSnapshot();

        await RunEvents<VoteResultEntered>(false);

        await StGallenErfassungElectionAdminClient.EnterResultsAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.EnterResultsAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterResultsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterResultsAsync(
                NewValidRequest(r => r.VoteResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterResultsAsync(
                NewValidRequest(r => r.VoteResultId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterResultsAsync(
                NewValidRequest(r => r.VoteResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenResult)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAlreadySubmitted()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterResultsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowUnrelatedBallotResults()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterResultsAsync(
                NewValidRequest(r =>
                    r.Results[0].BallotId = VoteMockedData.BallotIdBundVoteInContestBund)),
            StatusCode.InvalidArgument,
            "unknown results provided");
    }

    [Fact]
    public async Task TestShouldThrowUnrelatedQuestionResults()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterResultsAsync(
                NewValidRequest(r =>
                    r.Results[0].QuestionResults[0].QuestionNumber = 10)),
            StatusCode.InvalidArgument,
            "unknown results provided");
    }

    [Fact]
    public async Task TestShouldThrowCountOfUnspecifiedForStandardBallot()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterResultsAsync(
                new EnterVoteResultsRequest
                {
                    VoteResultId = VoteResultMockedData.IdGossauVoteInContestGossauResult,
                    Results =
                    {
                            new EnterVoteBallotResultsRequest
                            {
                                BallotId = VoteMockedData.BallotIdGossauVoteInContestGossau,
                                CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                                {
                                    ConventionalReceivedBallots = 10000,
                                    ConventionalAccountedBallots = 2000,
                                    ConventionalBlankBallots = 1000,
                                    ConventionalInvalidBallots = 500,
                                },
                                QuestionResults =
                                {
                                    new EnterVoteBallotQuestionResultRequest
                                    {
                                        QuestionNumber = 1,
                                        ReceivedCountYes = 1000,
                                        ReceivedCountNo = 580,
                                        ReceivedCountUnspecified = 10,
                                    },
                                },
                            },
                    },
                }),
            StatusCode.InvalidArgument,
            "unspecified answers are not allowed for standard ballots");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultEntered
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                Results =
                {
                        new VoteBallotResultsEventData
                        {
                            BallotId = VoteMockedData.BallotIdGossauVoteInContestStGallen,
                            QuestionResults =
                            {
                                new VoteBallotQuestionResultsEventData
                                {
                                    QuestionNumber = 1,
                                    ReceivedCountYes = 1000,
                                    ReceivedCountNo = 590,
                                },
                                new VoteBallotQuestionResultsEventData
                                {
                                    QuestionNumber = 2,
                                    ReceivedCountYes = 666,
                                    ReceivedCountNo = 123,
                                },
                            },
                            TieBreakQuestionResults =
                            {
                                new VoteTieBreakQuestionResultsEventData
                                {
                                    QuestionNumber = 1,
                                    ReceivedCountQ1 = 300,
                                    ReceivedCountQ2 = 200,
                                },
                            },
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        var result = await ErfassungCreatorClient.GetAsync(new GetVoteResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        });
        result.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .EnterResultsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private EnterVoteResultsRequest NewValidRequest(Action<EnterVoteResultsRequest>? customizer = null)
    {
        var r = new EnterVoteResultsRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            Results =
                {
                    new EnterVoteBallotResultsRequest
                    {
                        BallotId = VoteMockedData.BallotIdGossauVoteInContestStGallen,
                        CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                        {
                            ConventionalReceivedBallots = 10000,
                            ConventionalAccountedBallots = 2000,
                            ConventionalBlankBallots = 1000,
                            ConventionalInvalidBallots = 500,
                        },
                        QuestionResults =
                        {
                            new EnterVoteBallotQuestionResultRequest
                            {
                                QuestionNumber = 1,
                                ReceivedCountYes = 1000,
                                ReceivedCountNo = 590,
                            },
                            new EnterVoteBallotQuestionResultRequest
                            {
                                QuestionNumber = 2,
                                ReceivedCountYes = 666,
                                ReceivedCountNo = 123,
                            },
                        },
                        TieBreakQuestionResults =
                        {
                            new EnterVoteTieBreakQuestionResultRequest
                            {
                                QuestionNumber = 1,
                                ReceivedCountQ1 = 300,
                                ReceivedCountQ2 = 200,
                            },
                        },
                    },
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
