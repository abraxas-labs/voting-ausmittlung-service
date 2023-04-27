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
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultValidateEnterCountOfVotersTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";

    public VoteResultValidateEnterCountOfVotersTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        await UpdateResult(result =>
        {
            var ballotResult = result.Results.First();
            var questionResults = ballotResult.QuestionResults.ToList();
            var tieBreakQuestionResults = ballotResult.TieBreakQuestionResults.ToList();

            questionResults[0].ConventionalSubTotal.TotalCountOfAnswerYes = 2500;
            questionResults[0].ConventionalSubTotal.TotalCountOfAnswerNo = 1450;
            questionResults[0].ConventionalSubTotal.TotalCountOfAnswerUnspecified = 50;

            questionResults[1].ConventionalSubTotal.TotalCountOfAnswerYes = 3500;
            questionResults[1].ConventionalSubTotal.TotalCountOfAnswerNo = 500;
            questionResults[1].ConventionalSubTotal.TotalCountOfAnswerUnspecified = 0;

            tieBreakQuestionResults[0].ConventionalSubTotal.TotalCountOfAnswerQ1 = 2000;
            tieBreakQuestionResults[0].ConventionalSubTotal.TotalCountOfAnswerQ2 = 1900;
            tieBreakQuestionResults[0].ConventionalSubTotal.TotalCountOfAnswerUnspecified = 100;
        });

        await ModifyDbEntities(
            (Contest c) => c.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting),
            c => c.EVotingResultsImported = true);
    }

    [Fact]
    public async Task ShouldReturnIsValid()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();

        result.ValidationResults.Any(r => r.Validation
                is SharedProto.Validation.VoteQnCountOfAnswerNotNull
                or SharedProto.Validation.VoteTieBreakQnCountOfAnswerNotNull)
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task ShouldReturnComparisonVoterParticipations()
    {
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var ccId = CountingCircleMockedData.GuidStGallen;
        Func<ProtoModels.ValidationResult, bool> selector = x => x.Validation == SharedProto.Validation.ComparisonVoterParticipations;

        await SetReadModelResultStateForAllResultsInContest(contestId, CountingCircleResultState.SubmissionOngoing);

        var (_, res1) = await ValidateAndSubmitResult(
            Guid.Parse(VoteMockedData.IdBundVoteInContestBund),
            ccId,
            100,
            new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalAccountedBallots = 50,
                ConventionalBlankBallots = 0,
                ConventionalInvalidBallots = 1,
                ConventionalReceivedBallots = 51,
            },
            selector);

        res1.Any().Should().BeFalse();

        var (_, res2) = await ValidateAndSubmitResult(
            Guid.Parse(VoteMockedData.IdStGallenVoteInContestBund),
            ccId,
            100,
            new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalAccountedBallots = 60,
                ConventionalBlankBallots = 0,
                ConventionalInvalidBallots = 0,
                ConventionalReceivedBallots = 60,
            },
            selector);

        res2.Should().HaveCount(1);
        res2.MatchSnapshot("result2");
    }

    [Fact]
    public async Task ShouldReturnComparisonVotingCardsAndValidBallots()
    {
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var ccId = CountingCircleMockedData.GuidStGallen;

        await SetReadModelResultStateForAllResultsInContest(contestId, CountingCircleResultState.SubmissionOngoing);

        var (_, res) = await ValidateAndSubmitResult(
            Guid.Parse(VoteMockedData.IdBundVoteInContestBund),
            ccId,
            100,
            new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalAccountedBallots = 90,
                ConventionalBlankBallots = 0,
                ConventionalInvalidBallots = 1,
                ConventionalReceivedBallots = 91,
            },
            x => x.Validation == SharedProto.Validation.ComparisonValidVotingCardsWithAccountedBallots);

        res.Should().HaveCount(1);
        res.Should().MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotReceivedBallotsLessOrEqualValidVotingCards()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.ResultsCountOfVoters[0].CountOfVoters.ConventionalReceivedBallots = 20000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessReceivedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsLessOrEqualValidVotingCards()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.ResultsCountOfVoters[0].CountOfVoters.ConventionalAccountedBallots = 18000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.ResultsCountOfVoters[0].CountOfVoters.ConventionalAccountedBallots = 9000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotBundlesNotInProcess()
    {
        await UpdateResult(result =>
        {
            var ballotResult = result.Results.First();
            ballotResult.CountOfBundlesNotReviewedOrDeleted = 1;
        });

        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessBundlesNotInProcess)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsEqualCountOfAnswer()
    {
        await UpdateResult(result =>
        {
            var ballotResult = result.Results.First();
            var questionResults = ballotResult.QuestionResults.ToList();

            questionResults[0].ConventionalSubTotal.TotalCountOfAnswerYes--;
            questionResults[1].ConventionalSubTotal.TotalCountOfAnswerNo++;
            ballotResult.TieBreakQuestionResults.First().ConventionalSubTotal.TotalCountOfAnswerQ1++;
        });

        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());

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
    public async Task ShouldReturnIsValidAsContestManagerDuringTestingPhase()
    {
        var result = await StGallenErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();

        result.ValidationResults.Any(r => r.Validation
                is SharedProto.Validation.VoteQnCountOfAnswerNotNull
                or SharedProto.Validation.VoteTieBreakQnCountOfAnswerNotNull)
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
                x.Request.VoteResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenResult)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
                x.Request.VoteResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .ValidateEnterCountOfVotersAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private ValidateEnterVoteResultCountOfVotersRequest NewValidRequest(
        Action<ValidateEnterVoteResultCountOfVotersRequest>? customizer = null)
    {
        var r = new ValidateEnterVoteResultCountOfVotersRequest
        {
            Request = new EnterVoteResultCountOfVotersRequest
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                ResultsCountOfVoters =
                    {
                        new EnterVoteBallotResultsCountOfVotersRequest
                        {
                            BallotId = VoteMockedData.BallotIdGossauVoteInContestStGallen,
                            CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                            {
                                ConventionalReceivedBallots = 6000,
                                ConventionalAccountedBallots = 4000,
                                ConventionalBlankBallots = 1500,
                                ConventionalInvalidBallots = 500,
                            },
                        },
                    },
            },
        };

        customizer?.Invoke(r);
        return r;
    }

    private async Task UpdateResult(Action<VoteResult> customizer)
    {
        await RunOnDb(async db =>
        {
            var result = await db.VoteResults
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.Results).ThenInclude(x => x.QuestionResults).ThenInclude(x => x.Question)
                .Include(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults).ThenInclude(x => x.Question)
                .FirstAsync(x => x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult));

            foreach (var ballotResult in result.Results)
            {
                ballotResult.OrderQuestionResultsAndSubTotals();
            }

            customizer(result);

            await db.SaveChangesAsync();
        });
    }

    private async Task<(Guid PbResId, List<ProtoModels.ValidationResult> ValidationResults)> ValidateAndSubmitResult(
        Guid pbId,
        Guid ccId,
        int totalCountOfVoters,
        EnterPoliticalBusinessCountOfVotersRequest countOfVotersProto,
        Func<ProtoModels.ValidationResult, bool> validationResultSelector)
    {
        var mapper = GetService<TestMapper>();

        var pbRes = await RunOnDb(async db =>
            (await db.VoteResults
                .Include(x => x.CountingCircle)
                .Include(x => x.Results)
                .ToListAsync())
                .Single(x => x.CountingCircle.BasisCountingCircleId == ccId && x.PoliticalBusinessId == pbId));
        var pbResId = pbRes.Id;
        var ballotId = pbRes.Results.Single().BallotId;

        var pbResRequest = new EnterVoteResultCountOfVotersRequest
        {
            VoteResultId = pbResId.ToString(),
            ResultsCountOfVoters =
                {
                    new EnterVoteBallotResultsCountOfVotersRequest
                    {
                        BallotId = ballotId.ToString(),
                        CountOfVoters = countOfVotersProto,
                    },
                },
        };

        var pbResValidateResponse = await StGallenErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(new ValidateEnterVoteResultCountOfVotersRequest
        {
            Request = pbResRequest,
        });

        var countOfVoters = mapper.Map<PoliticalBusinessNullableCountOfVoters>(pbResRequest.ResultsCountOfVoters[0].CountOfVoters);
        countOfVoters.UpdateVoterParticipation(totalCountOfVoters);

        await RunOnDb(async db =>
        {
            var voteResult = await db.VoteResults.AsTracking().Include(x => x.Results).SingleAsync(x => x.Id == pbResId);
            voteResult.State = CountingCircleResultState.SubmissionDone;
            voteResult.Results.Single().CountOfVoters = countOfVoters;
            await db.SaveChangesAsync();
        });

        return (
            pbResId,
            pbResValidateResponse.ValidationResults
                .Where(validationResultSelector)
                .ToList());
    }
}
