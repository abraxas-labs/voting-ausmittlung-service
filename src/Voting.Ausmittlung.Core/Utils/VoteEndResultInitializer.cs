// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Core.Utils;

public class VoteEndResultInitializer
{
    private readonly VoteRepo _voteRepo;
    private readonly VoteEndResultRepo _endResultRepo;
    private readonly BallotRepo _ballotRepo;
    private readonly DataContext _dataContext;

    public VoteEndResultInitializer(
        VoteRepo voteRepo,
        VoteEndResultRepo endResultRepo,
        BallotRepo ballotRepo,
        DataContext dataContext)
    {
        _voteRepo = voteRepo;
        _endResultRepo = endResultRepo;
        _ballotRepo = ballotRepo;
        _dataContext = dataContext;
    }

    internal async Task RebuildForVote(Guid voteId, bool testingPhaseEnded)
    {
        var endResultId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(voteId, testingPhaseEnded);
        var countOfCountingCircles = await _voteRepo.CountOfCountingCircles(voteId);

        var voteEndResult = await _endResultRepo.Query()
            .FirstOrDefaultAsync(r => r.VoteId == voteId);

        if (testingPhaseEnded && voteEndResult != null)
        {
            if (voteEndResult.Id == endResultId)
            {
                throw new InvalidOperationException("Cannot build end result after testing phase ended when it is already built");
            }

            await _endResultRepo.DeleteByKey(voteEndResult.Id);
            voteEndResult = null;
        }

        if (voteEndResult == null)
        {
            voteEndResult = new VoteEndResult
            {
                Id = endResultId,
                VoteId = voteId,
                TotalCountOfCountingCircles = countOfCountingCircles,
            };
            await _endResultRepo.Create(voteEndResult);
        }
        else
        {
            voteEndResult.TotalCountOfCountingCircles = countOfCountingCircles;
            await _endResultRepo.Update(voteEndResult);
        }

        var ballots = await _ballotRepo.GetByVoteIdWithEndResultsAsTracked(voteId);
        foreach (var ballot in ballots)
        {
            AddMissingEndResultsToBallot(ballot);
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task RebuildForBallot(Guid ballotId)
    {
        var ballot = await _ballotRepo.GetWithEndResultsAsTracked(ballotId)
                     ?? throw new EntityNotFoundException(ballotId);

        AddMissingEndResultsToBallot(ballot);

        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetForVote(Guid voteId)
    {
        var voteEndResultId = await _endResultRepo.Query()
               .Where(vr => vr.VoteId == voteId)
               .Select(vr => vr.Id)
               .FirstOrDefaultAsync();

        await _endResultRepo.DeleteByKey(voteEndResultId);
        await RebuildForVote(voteId, true);
    }

    private void AddMissingEndResultsToBallot(Ballot ballot)
    {
        if (ballot.EndResult == null)
        {
            ballot.EndResult = new BallotEndResult
            {
                VoteEndResult = ballot.Vote.EndResult!,
            };
        }

        AddMissingBallotQuestionEndResults(ballot.EndResult, ballot.BallotQuestions);
        AddMissingTieBreakQuestionEndResults(ballot.EndResult, ballot.TieBreakQuestions);
    }

    private void AddMissingBallotQuestionEndResults(BallotEndResult endResult, IEnumerable<BallotQuestion> questions)
    {
        var existingQuestionEndResultQuestionIds = endResult.QuestionEndResults.Select(qr => qr.QuestionId).ToList();
        var newBallotQuestionEndResults = questions.Where(b => !existingQuestionEndResultQuestionIds.Contains(b.Id))
            .Select(bq => new BallotQuestionEndResult
            {
                QuestionId = bq.Id,
                BallotEndResultId = endResult.Id,
            });
        foreach (var newBallotQuestionEndResult in newBallotQuestionEndResults)
        {
            endResult.QuestionEndResults.Add(newBallotQuestionEndResult);
        }
    }

    private void AddMissingTieBreakQuestionEndResults(BallotEndResult endResult, IEnumerable<TieBreakQuestion> questions)
    {
        var existingQuestionEndResultQuestionIds = endResult.TieBreakQuestionEndResults.Select(qr => qr.QuestionId).ToList();
        var newTieBreakQuestionEndResults = questions.Where(b => !existingQuestionEndResultQuestionIds.Contains(b.Id))
            .Select(bq => new TieBreakQuestionEndResult
            {
                QuestionId = bq.Id,
                BallotEndResultId = endResult.Id,
            });
        foreach (var newBallotQuestionEndResult in newTieBreakQuestionEndResults)
        {
            endResult.TieBreakQuestionEndResults.Add(newBallotQuestionEndResult);
        }
    }
}
