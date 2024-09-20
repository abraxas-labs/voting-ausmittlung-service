// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultResetTest : BaseIntegrationTest
{
    private static readonly Guid ResultId = VoteResultMockedData.GuidGossauVoteInContestStGallenResult;
    private static readonly Guid BallotResultId = VoteResultMockedData.GuidGossauVoteInContestStGallenBallotResult;

    public VoteResultResetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);

        await ModifyDbEntities<VoteResult>(
            r => r.Id == ResultId,
            r => r.State = CountingCircleResultState.CorrectionDone);

        await ModifyDbEntities<BallotResult>(
            r => r.Id == BallotResultId,
            r =>
            {
                r.CountOfVoters.EVotingReceivedBallots = 5;
                r.CountOfVoters.EVotingAccountedBallots = 5;
                r.ConventionalCountOfDetailedEnteredBallots = 2;
                r.CountOfBundlesNotReviewedOrDeleted = 1;
            });

        await ModifyDbEntities<BallotQuestionResult>(
            r => r.BallotResultId == BallotResultId,
            r => r.EVotingSubTotal = new()
            {
                TotalCountOfAnswerYes = 4,
                TotalCountOfAnswerNo = 1,
                TotalCountOfAnswerUnspecified = 1,
            });

        await ModifyDbEntities<TieBreakQuestionResult>(
            r => r.BallotResultId == BallotResultId,
            r => r.EVotingSubTotal = new()
            {
                TotalCountOfAnswerQ1 = 4,
                TotalCountOfAnswerQ2 = 1,
                TotalCountOfAnswerUnspecified = 1,
            });

        await RunOnDb(async db =>
        {
            db.VoteResultBundles.Add(new()
            {
                Id = Guid.Parse("1fe63a07-a6dd-4871-bc78-70e7829a07a3"),
                BallotResultId = BallotResultId,
                CountOfBallots = 2,
                BallotNumbersToReview = new() { 1 },
            });
            await db.SaveChangesAsync();
        });

        await base.InitializeAsync();
    }

    [Fact]
    public async Task TestReset()
    {
        var result = await LoadResult();
        result.State.Should().Be(CountingCircleResultState.CorrectionDone);
        var ballotResult = result.Results.Single();
        ballotResult.Bundles.Any().Should().BeTrue();
        ballotResult.Bundles = null!;
        result.MatchSnapshot("resultBefore");

        await TestEventPublisher.Publish(new VoteResultResetted
        {
            EventInfo = GetMockedEventInfo(),
            VoteResultId = ResultId.ToString(),
        });

        result = await LoadResult();
        result.State.Should().Be(CountingCircleResultState.SubmissionOngoing);
        ballotResult = result.Results.Single();
        ballotResult.Bundles.Any().Should().BeFalse();
        ballotResult.Bundles = null!;
        result.MatchSnapshot("resultAfter");
    }

    private async Task<VoteResult> LoadResult()
    {
        var result = await RunOnDb(
            db => db
                .VoteResults
                .AsSplitQuery()
                .Include(r => r.Results)
                    .ThenInclude(r => r.QuestionResults.OrderBy(x => x.Question.Number))
                .Include(r => r.Results)
                    .ThenInclude(r => r.TieBreakQuestionResults.OrderBy(x => x.Question.Number))
                .Include(r => r.Results)
                    .ThenInclude(r => r.Bundles)
                .SingleAsync(r => r.Id == ResultId));

        var ballotResult = result.Results.Single();
        SetDynamicIdToDefaultValue(ballotResult.QuestionResults);
        SetDynamicIdToDefaultValue(ballotResult.TieBreakQuestionResults);

        return result;
    }
}
