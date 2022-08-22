// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class VoteEndResultRepo : DbRepository<DataContext, VoteEndResult>
{
    public VoteEndResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<VoteEndResult?> GetByVoteIdAsTracked(Guid voteId)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.Vote)
            .Include(b => b.BallotEndResults).ThenInclude(r => r.QuestionEndResults)
            .Include(b => b.BallotEndResults).ThenInclude(r => r.TieBreakQuestionEndResults)
            .FirstOrDefaultAsync(b => b.VoteId == voteId);
    }

    public Task<List<VoteEndResult>> ListWithResultsByContestIdAsTracked(Guid contestId)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.BallotEndResults)
            .ThenInclude(x => x.QuestionEndResults)
            .Include(x => x.BallotEndResults)
            .ThenInclude(x => x.TieBreakQuestionEndResults)
            .Include(x => x.Vote.Results)
            .ThenInclude(x => x.Results)
            .ThenInclude(x => x.QuestionResults)
            .Include(x => x.Vote.Results)
            .ThenInclude(x => x.Results)
            .ThenInclude(x => x.TieBreakQuestionResults)
            .Where(x => x.Vote.ContestId == contestId)
            .ToListAsync();
    }
}
