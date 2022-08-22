// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultValidateEnterResultsTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";

    public VoteResultValidateEnterResultsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ModifyDbEntities(
            (Contest c) => c.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting),
            c => c.EVotingResultsImported = true);
        await ModifyDbEntities(
            (VoteResult vr) => vr.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult),
            vr => vr.Entry = VoteResultEntry.FinalResults);
    }

    [Fact]
    public async Task ShouldReturnIsValid()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterResultsAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotReceivedBallotsLessOrEqualValidVotingCards()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterResultsAsync(NewValidRequest(x =>
            x.Request.Results[0].CountOfVoters.ConventionalReceivedBallots = 20000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessReceivedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsLessOrEqualValidVotingCards()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterResultsAsync(NewValidRequest(x =>
            x.Request.Results[0].CountOfVoters.ConventionalAccountedBallots = 18000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterResultsAsync(NewValidRequest(x =>
            x.Request.Results[0].CountOfVoters.ConventionalAccountedBallots = 9000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotVoteQnCountOfAnswerNotNull()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterResultsAsync(NewValidRequest(x =>
        {
            x.Request.Results[0].QuestionResults[0].ReceivedCountNo = null;
            x.Request.Results[0].TieBreakQuestionResults[0].ReceivedCountQ1 = null;
        }));

        result.ValidationResults.Count(r => r.Validation == SharedProto.Validation.VoteQnCountOfAnswerNotNull)
            .Should()
            .Be(2);

        result.ValidationResults.Count(r => r.Validation == SharedProto.Validation.VoteQnCountOfAnswerNotNull && r.IsValid)
            .Should()
            .Be(1);

        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.VoteTieBreakQnCountOfAnswerNotNull)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsEqualCountOfAnswer()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterResultsAsync(NewValidRequest(x =>
        {
            var ballotResult = x.Request.Results[0];
            ballotResult.QuestionResults[0].ReceivedCountNo--;
            ballotResult.QuestionResults[1].ReceivedCountUnspecified++;
            ballotResult.TieBreakQuestionResults[0].ReceivedCountQ2--;
        }));

        result.ValidationResults.Count(r => r.Validation == SharedProto.Validation.VoteAccountedBallotsEqualQnCountOfAnswer)
            .Should()
            .Be(2);

        result.ValidationResults.All(r => r.Validation == SharedProto.Validation.VoteAccountedBallotsEqualQnCountOfAnswer)
            .Should()
            .BeFalse();

        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.VoteAccountedBallotsEqualTieBreakQnCountOfAnswer)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterResultsAsync(NewValidRequest(x =>
                x.Request.VoteResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenResult)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterResultsAsync(NewValidRequest(x =>
                x.Request.VoteResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .ValidateEnterResultsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private ValidateEnterVoteResultsRequest NewValidRequest(Action<ValidateEnterVoteResultsRequest>? customizer = null)
    {
        var r = new ValidateEnterVoteResultsRequest
        {
            Request = new EnterVoteResultsRequest
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                Results =
                    {
                        new EnterVoteBallotResultsRequest
                        {
                            BallotId = VoteMockedData.BallotIdGossauVoteInContestStGallen,
                            CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                            {
                                ConventionalReceivedBallots = 6000,
                                ConventionalAccountedBallots = 4000,
                                ConventionalBlankBallots = 1500,
                                ConventionalInvalidBallots = 500,
                            },
                            QuestionResults =
                            {
                                new EnterVoteBallotQuestionResultRequest
                                {
                                    QuestionNumber = 1,
                                    ReceivedCountYes = 3200,
                                    ReceivedCountNo = 800,
                                    ReceivedCountUnspecified = 0,
                                },
                                new EnterVoteBallotQuestionResultRequest
                                {
                                    QuestionNumber = 2,
                                    ReceivedCountYes = 100,
                                    ReceivedCountNo = 3800,
                                    ReceivedCountUnspecified = 100,
                                },
                            },
                            TieBreakQuestionResults =
                            {
                                new EnterVoteTieBreakQuestionResultRequest
                                {
                                    QuestionNumber = 1,
                                    ReceivedCountQ1 = 2000,
                                    ReceivedCountQ2 = 2000,
                                    ReceivedCountUnspecified = 0,
                                },
                            },
                        },
                    },
            },
        };

        customizer?.Invoke(r);
        return r;
    }
}
