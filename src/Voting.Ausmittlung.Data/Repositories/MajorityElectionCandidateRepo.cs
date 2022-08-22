// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class MajorityElectionCandidateRepo : DbRepository<DataContext, MajorityElectionCandidate>
{
    public MajorityElectionCandidateRepo(DataContext context)
        : base(context)
    {
    }

    public Task<MajorityElectionCandidate?> GetWithEndResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .Include(x => x.EndResult)
            .Include(x => x.MajorityElection.EndResult)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
