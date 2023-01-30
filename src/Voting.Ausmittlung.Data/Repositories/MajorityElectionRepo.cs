// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class MajorityElectionRepo : PoliticalBusinessRepo<MajorityElection>
{
    public MajorityElectionRepo(DataContext context)
        : base(context)
    {
    }

    public Task<MajorityElection?> GetWithEndResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.MajorityElectionCandidates)
            .Include(x => x.SecondaryMajorityElections).ThenInclude(x => x.Candidates)
            .Include(x => x.EndResult!.CandidateEndResults)
            .Include(x => x.EndResult!.SecondaryMajorityElectionEndResults).ThenInclude(x => x.CandidateEndResults)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public override IQueryable<MajorityElection> QueryWithResults()
    {
        return Query().AsSplitQuery().Include(me => me.Results).ThenInclude(r => r.CountingCircle);
    }
}
