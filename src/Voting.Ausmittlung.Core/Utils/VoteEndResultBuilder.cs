// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Core.Utils;

public class VoteEndResultBuilder
{
    private readonly VoteEndResultRepo _endResultRepo;
    private readonly VoteResultRepo _resultRepo;
    private readonly DataContext _dbContext;

    public VoteEndResultBuilder(VoteEndResultRepo endResultRepo, VoteResultRepo resultRepo, DataContext dbContext)
    {
        _endResultRepo = endResultRepo;
        _resultRepo = resultRepo;
        _dbContext = dbContext;
    }

    internal async Task ResetAllResults(Guid contestId, VotingDataSource dataSource)
    {
        var endResults = await _endResultRepo.ListWithResultsByContestIdAsTracked(contestId);

        foreach (var endResult in endResults)
        {
            endResult.ResetAllSubTotals(dataSource, true);

            foreach (var result in endResult.Vote.Results)
            {
                result.ResetAllSubTotals(dataSource, true);
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    internal async Task AdjustVoteEndResult(Guid voteResultId, bool removeResults)
    {
        var deltaFactor = removeResults ? -1 : 1;

        var voteResult = await _resultRepo.GetVoteResultWithRelations(voteResultId)
                         ?? throw new EntityNotFoundException(nameof(VoteResult), voteResultId);

        var voteEndResult = await _endResultRepo.GetByVoteIdAsTracked(voteResult.VoteId)
                            ?? throw new EntityNotFoundException(nameof(VoteEndResult), voteResult.VoteId);

        voteEndResult.CountOfDoneCountingCircles += deltaFactor;
        voteEndResult.TotalCountOfVoters += voteResult.TotalCountOfVoters * deltaFactor;
        voteEndResult.Finalized = false;

        voteEndResult.BallotEndResults.MatchAndExec(
            b => b.BallotId,
            voteResult.Results,
            b => b.BallotId,
            (endResult, result) =>
            {
                PoliticalBusinessCountOfVotersUtils.AdjustCountOfVoters(
                    endResult.CountOfVoters,
                    result.CountOfVoters,
                    voteEndResult.TotalCountOfVoters,
                    deltaFactor);

                AdjustBallotEndResult(endResult, result, voteEndResult, deltaFactor);
            });

        await _dbContext.SaveChangesAsync();
    }

    private void AdjustBallotEndResult(
        BallotEndResult ballotEndResult,
        BallotResult ballotResult,
        VoteEndResult voteEndResult,
        int deltaFactor)
    {
        ballotEndResult.QuestionEndResults.MatchAndExec(
            x => x.QuestionId,
            ballotResult.QuestionResults,
            x => x.QuestionId,
            (endResult, result) => AdjustQuestionEndResult(endResult, result, voteEndResult.TotalCountOfCountingCircles, voteEndResult.Vote.ResultAlgorithm, deltaFactor));
        ballotEndResult.TieBreakQuestionEndResults.MatchAndExec(
            x => x.QuestionId,
            ballotResult.TieBreakQuestionResults,
            x => x.QuestionId,
            (endResult, result) => AdjustTieBreakQuestionEndResult(endResult, result, voteEndResult.TotalCountOfCountingCircles, voteEndResult.Vote.ResultAlgorithm, deltaFactor));
    }

    private void AdjustQuestionEndResult(
        BallotQuestionEndResult endResult,
        BallotQuestionResult result,
        int totalCountOfCountingCircles,
        VoteResultAlgorithm algorithm,
        int deltaFactor)
    {
        if (result.HasMajority)
        {
            endResult.CountOfCountingCircleYes += deltaFactor;
        }
        else
        {
            endResult.CountOfCountingCircleNo += deltaFactor;
        }

        endResult.ForEachSubTotal(result, (endResultSubTotal, resultSubTotal) =>
        {
            endResultSubTotal.TotalCountOfAnswerYes += resultSubTotal.TotalCountOfAnswerYes * deltaFactor;
            endResultSubTotal.TotalCountOfAnswerNo += resultSubTotal.TotalCountOfAnswerNo * deltaFactor;
            endResultSubTotal.TotalCountOfAnswerUnspecified += resultSubTotal.TotalCountOfAnswerUnspecified * deltaFactor;
        });

        endResult.HasCountingCircleMajority = endResult.CountOfCountingCircleYes > totalCountOfCountingCircles / 2;
        endResult.HasCountingCircleUnanimity = endResult.CountOfCountingCircleYes == totalCountOfCountingCircles;
        UpdateIsAccepted(endResult, algorithm);
    }

    private void AdjustTieBreakQuestionEndResult(
        TieBreakQuestionEndResult endResult,
        TieBreakQuestionResult result,
        int totalCountOfCountingCircles,
        VoteResultAlgorithm algorithm,
        int deltaFactor)
    {
        if (result.HasQ1Majority)
        {
            endResult.CountOfCountingCircleQ1 += deltaFactor;
        }
        else if (result.HasQ2Majority)
        {
            endResult.CountOfCountingCircleQ2 += deltaFactor;
        }

        endResult.ForEachSubTotal(result, (endResultSubTotal, resultSubTotal) =>
        {
            endResultSubTotal.TotalCountOfAnswerQ1 += resultSubTotal.TotalCountOfAnswerQ1 * deltaFactor;
            endResultSubTotal.TotalCountOfAnswerQ2 += resultSubTotal.TotalCountOfAnswerQ2 * deltaFactor;
            endResultSubTotal.TotalCountOfAnswerUnspecified += resultSubTotal.TotalCountOfAnswerUnspecified * deltaFactor;
        });

        endResult.HasCountingCircleQ1Majority = endResult.CountOfCountingCircleQ1 > totalCountOfCountingCircles / 2;
        endResult.HasCountingCircleQ2Majority = endResult.CountOfCountingCircleQ2 > totalCountOfCountingCircles / 2;

        UpdateQ1Accepted(endResult, algorithm);
    }

    private void UpdateIsAccepted(BallotQuestionEndResult questionEndResult, VoteResultAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case VoteResultAlgorithm.PopularMajority:
                questionEndResult.Accepted = questionEndResult.TotalCountOfAnswerYes > questionEndResult.TotalCountOfAnswerNo;
                break;
            case VoteResultAlgorithm.CountingCircleUnanimity:
                questionEndResult.Accepted = questionEndResult.HasCountingCircleUnanimity;
                break;
            case VoteResultAlgorithm.CountingCircleMajority:
                questionEndResult.Accepted = questionEndResult.HasCountingCircleMajority;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
        }
    }

    private void UpdateQ1Accepted(TieBreakQuestionEndResult questionEndResult, VoteResultAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case VoteResultAlgorithm.PopularMajority:
                questionEndResult.Q1Accepted = questionEndResult.TotalCountOfAnswerQ1 > questionEndResult.TotalCountOfAnswerQ2;
                break;

            // as defined by VOTING-416
            case VoteResultAlgorithm.CountingCircleUnanimity:
            case VoteResultAlgorithm.CountingCircleMajority:
                questionEndResult.Q1Accepted = questionEndResult.HasCountingCircleQ1Majority;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
        }
    }
}
