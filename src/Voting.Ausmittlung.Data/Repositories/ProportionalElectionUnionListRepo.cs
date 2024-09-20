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

public class ProportionalElectionUnionListRepo : DbRepository<DataContext, ProportionalElectionUnionList>
{
    public ProportionalElectionUnionListRepo(DataContext context)
        : base(context)
    {
    }

    public async Task Replace(List<Guid> proportionalElectionUnionIds, List<ProportionalElectionUnionList> entries)
    {
        var existingEntries = await Set.Where(e => proportionalElectionUnionIds.Contains(e.ProportionalElectionUnionId)).ToArrayAsync();

        Set.RemoveRange(existingEntries);
        await Context.SaveChangesAsync();

        Set.AddRange(entries);
        await Context.SaveChangesAsync();
    }
}
