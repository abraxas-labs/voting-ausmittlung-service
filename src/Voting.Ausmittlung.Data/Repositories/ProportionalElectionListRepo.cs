// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionListRepo : DbRepository<DataContext, ProportionalElectionList>
{
    public ProportionalElectionListRepo(DataContext context)
        : base(context)
    {
    }

    public Task<ProportionalElectionList?> GetWithEndResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.ProportionalElection.EndResult)
            .Include(x => x.ProportionalElectionCandidates)
            .Include(x => x.EndResult!.CandidateEndResults)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
