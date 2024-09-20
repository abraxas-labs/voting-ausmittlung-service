// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class BallotRepo : DbRepository<DataContext, Ballot>
{
    public BallotRepo(DataContext context)
        : base(context)
    {
    }

    public Task<Ballot?> GetWithResultsAsTracked(Guid id)
    {
        return QueryWithResultsAsTracked()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public Task<Ballot?> GetWithEndResultsAsTracked(Guid id)
    {
        return QueryWithEndResultsAsTracked()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public Task<List<Ballot>> GetByVoteIdWithResultsAsTracked(Guid voteId)
    {
        return QueryWithResultsAsTracked()
            .Where(b => b.VoteId == voteId)
            .ToListAsync();
    }

    public Task<List<Ballot>> GetByVoteIdWithEndResultsAsTracked(Guid voteId)
    {
        return QueryWithEndResultsAsTracked()
            .Where(b => b.VoteId == voteId)
            .ToListAsync();
    }

    public IQueryable<Ballot> QueryWithResultsAsTracked()
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(b => b.BallotQuestions)
            .Include(b => b.TieBreakQuestions)
            .Include(b => b.Results).ThenInclude(r => r.QuestionResults).ThenInclude(qr => qr.Question)
            .Include(b => b.Results).ThenInclude(r => r.TieBreakQuestionResults).ThenInclude(r => r.Question);
    }

    public IQueryable<Ballot> QueryWithEndResultsAsTracked()
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(b => b.Vote.EndResult)
            .Include(b => b.EndResult!.VoteEndResult)
            .Include(b => b.BallotQuestions)
            .Include(b => b.TieBreakQuestions)
            .Include(b => b.EndResult).ThenInclude(r => r!.QuestionEndResults).ThenInclude(qr => qr.Question)
            .Include(b => b.EndResult).ThenInclude(r => r!.TieBreakQuestionEndResults).ThenInclude(r => r.Question);
    }
}
