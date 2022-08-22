// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class DomainOfInfluenceCountingCircleRepo : DbRepository<DataContext, DomainOfInfluenceCountingCircle>
{
    public DomainOfInfluenceCountingCircleRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<Dictionary<Guid, List<DomainOfInfluenceCountingCircle>>> CountingCirclesByDomainOfInfluenceId()
    {
        var entries = await Query()
            .WhereContestIsInTestingPhase()
            .Include(c => c.CountingCircle)
            .ThenInclude(c => c.ResponsibleAuthority)
            .OrderBy(c => c.CountingCircle.Name)
            .ToListAsync();

        return entries
            .GroupBy(x => x.DomainOfInfluenceId)
            .ToDictionary(x => x.Key, x => x.ToList());
    }

    public async Task AddRange(IEnumerable<DomainOfInfluenceCountingCircle> entries)
    {
        Set.AddRange(entries);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes all Entries, where any of the DomainOfInfluenceIds matches any of the CountingCircleIds.
    /// </summary>
    /// <param name="domainOfInfluenceIds">DomainofInfluenceIds to delete.</param>
    /// <param name="countingCircleIds">CountingCircleIds to delete.</param>
    /// <returns>A Task.</returns>
    public async Task RemoveAll(List<Guid> domainOfInfluenceIds, List<Guid> countingCircleIds)
    {
        if (domainOfInfluenceIds.Count == 0 || countingCircleIds.Count == 0)
        {
            return;
        }

        var existingEntries = await Query()
            .Where(doiCc => domainOfInfluenceIds.Contains(doiCc.DomainOfInfluenceId) && countingCircleIds.Contains(doiCc.CountingCircleId))
            .ToListAsync();

        Set.RemoveRange(existingEntries);
        await Context.SaveChangesAsync();
    }
}
