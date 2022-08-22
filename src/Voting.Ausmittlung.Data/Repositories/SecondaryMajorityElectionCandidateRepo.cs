// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class SecondaryMajorityElectionCandidateRepo : DbRepository<DataContext, SecondaryMajorityElectionCandidate>
{
    public SecondaryMajorityElectionCandidateRepo(DataContext context)
        : base(context)
    {
    }

    public Task<SecondaryMajorityElectionCandidate?> GetWithEndResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .Include(x => x.EndResult)
            .Include(x => x.SecondaryMajorityElection.EndResult)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
