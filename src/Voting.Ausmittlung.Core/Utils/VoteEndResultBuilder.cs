// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class VoteEndResultBuilder
{
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;
    private readonly VoteEndResultRepo _endResultRepo;
    private readonly VoteResultRepo _resultRepo;
    private readonly DataContext _dbContext;

    public VoteEndResultBuilder(
        VoteEndResultRepo endResultRepo,
        VoteResultRepo resultRepo,
        DataContext dbContext,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo)
    {
        _endResultRepo = endResultRepo;
        _resultRepo = resultRepo;
        _dbContext = dbContext;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
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

        var countingCircleDetails = voteResult.CountingCircle.ContestDetails.FirstOrDefault()
            ?? throw new EntityNotFoundException(nameof(ContestDetails), voteResultId);

        var voteEndResult = await _endResultRepo.GetByVoteIdAsTracked(voteResult.VoteId)
            ?? throw new EntityNotFoundException(nameof(VoteEndResult), voteResult.VoteId);

        var simplePb = await _simplePoliticalBusinessRepo.Query()
                .AsSplitQuery()
                .AsTracking()
                .Include(x => x.DomainOfInfluence)
                .Include(x => x.Contest.CantonDefaults)
                .FirstOrDefaultAsync(x => x.Id == voteResult.VoteId)
            ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), voteResult.VoteId);

        voteEndResult.CountOfDoneCountingCircles += deltaFactor;

        var implicitFinalized = simplePb.Contest.CantonDefaults.EndResultFinalizeDisabled && voteEndResult.AllCountingCirclesDone;
        voteEndResult.Finalized = implicitFinalized;
        simplePb.EndResultFinalized = implicitFinalized;

        EndResultContestDetailsUtils.AdjustEndResultContestDetails<
            VoteEndResult,
            VoteEndResultCountOfVotersInformationSubTotal,
            VoteEndResultVotingCardDetail>(
                voteEndResult,
                countingCircleDetails,
                simplePb.DomainOfInfluence,
                deltaFactor);

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
        if (result.TotalCountOfAnswerNo != 0 || result.TotalCountOfAnswerYes != 0)
        {
            if (result.HasMajority)
            {
                endResult.CountOfCountingCircleYes += deltaFactor;
            }
            else
            {
                endResult.CountOfCountingCircleNo += deltaFactor;
            }
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
        questionEndResult.Accepted = algorithm switch
        {
            VoteResultAlgorithm.PopularMajority => questionEndResult.TotalCountOfAnswerYes > questionEndResult.TotalCountOfAnswerNo,
            VoteResultAlgorithm.CountingCircleUnanimity => questionEndResult.HasCountingCircleUnanimity,
            VoteResultAlgorithm.CountingCircleMajority => questionEndResult.HasCountingCircleMajority,
            VoteResultAlgorithm.PopularAndCountingCircleMajority => questionEndResult.TotalCountOfAnswerYes > questionEndResult.TotalCountOfAnswerNo && questionEndResult.HasCountingCircleMajority,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };
    }

    private void UpdateQ1Accepted(TieBreakQuestionEndResult questionEndResult, VoteResultAlgorithm algorithm)
    {
        questionEndResult.Q1Accepted = algorithm switch
        {
            VoteResultAlgorithm.PopularMajority => questionEndResult.TotalCountOfAnswerQ1 > questionEndResult.TotalCountOfAnswerQ2,

            // as defined by VOTING-416
            VoteResultAlgorithm.CountingCircleUnanimity or VoteResultAlgorithm.CountingCircleMajority => questionEndResult.HasCountingCircleQ1Majority,
            VoteResultAlgorithm.PopularAndCountingCircleMajority => questionEndResult.TotalCountOfAnswerQ1 > questionEndResult.TotalCountOfAnswerQ2 && questionEndResult.HasCountingCircleQ1Majority,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };
    }
}
