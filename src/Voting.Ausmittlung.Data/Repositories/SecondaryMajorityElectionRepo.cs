// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class SecondaryMajorityElectionRepo : DbRepository<DataContext, SecondaryMajorityElection>
{
    public SecondaryMajorityElectionRepo(DataContext context)
        : base(context)
    {
    }

    public Task<SecondaryMajorityElection?> GetWithEndResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.PrimaryMajorityElection.EndResult)
            .Include(x => x.Candidates)
            .Include(x => x.EndResult!.CandidateEndResults)
            .Include(x => x.EndResult!.PrimaryMajorityElectionEndResult)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
