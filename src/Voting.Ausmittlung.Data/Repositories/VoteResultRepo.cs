// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class VoteResultRepo : PoliticalBusinessResultRepo<VoteResult>
{
    public VoteResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<VoteResult?> GetVoteResultWithQuestionResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(b => b.Vote)
            .Include(b => b.Results).ThenInclude(r => r.Ballot)
            .Include(b => b.Results).ThenInclude(r => r.QuestionResults).ThenInclude(qr => qr.Question)
            .Include(b => b.Results).ThenInclude(r => r.TieBreakQuestionResults).ThenInclude(r => r.Question)
            .Include(b => b.Results).ThenInclude(r => r.Bundles)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public Task<VoteResult?> GetVoteResultWithRelations(Guid id)
    {
        return Set
            .AsSplitQuery()
            .Include(b => b.Results).ThenInclude(r => r.QuestionResults)
            .Include(b => b.Results).ThenInclude(r => r.TieBreakQuestionResults)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    protected override Expression<Func<VoteResult, bool>> FilterByPoliticalBusinessId(Guid id) =>
        vr => vr.VoteId == id;
}
