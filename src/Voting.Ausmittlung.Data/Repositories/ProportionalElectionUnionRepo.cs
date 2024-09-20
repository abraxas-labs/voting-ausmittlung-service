// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionUnionRepo : DbRepository<DataContext, ProportionalElectionUnion>
{
    public ProportionalElectionUnionRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<int> CountOfElections(Guid unionId)
    {
        return await Set.Where(x => x.Id == unionId)
            .SelectMany(x => x.ProportionalElectionUnionEntries)
            .Where(x => x.ProportionalElection.Active)
            .CountAsync();
    }

    public Task<ProportionalElectionUnion?> GetWithEndResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.ProportionalElectionUnionLists)
            .Include(x => x.EndResult)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
