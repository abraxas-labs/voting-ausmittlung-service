// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionRepo : PoliticalBusinessRepo<ProportionalElection>
{
    public ProportionalElectionRepo(DataContext context, DomainOfInfluenceRepo domainOfInfluenceRepo, CountingCircleRepo countingCircleRepo)
        : base(context, domainOfInfluenceRepo, countingCircleRepo)
    {
    }

    public Task<ProportionalElection?> GetWithEndResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.ProportionalElectionLists).ThenInclude(x => x.ProportionalElectionCandidates)
            .Include(x => x.EndResult!.ListEndResults).ThenInclude(x => x.CandidateEndResults)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public override IQueryable<ProportionalElection> QueryWithResults()
    {
        return Query().AsSplitQuery().Include(pe => pe.Results).ThenInclude(r => r.CountingCircle);
    }
}
