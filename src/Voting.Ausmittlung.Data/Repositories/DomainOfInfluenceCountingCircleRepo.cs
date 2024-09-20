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

public class DomainOfInfluenceCountingCircleRepo : DbRepository<DataContext, DomainOfInfluenceCountingCircle>
{
    public DomainOfInfluenceCountingCircleRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<Dictionary<Guid, List<CountingCircle>>> BasisCountingCirclesByDomainOfInfluenceId()
    {
        var entries = await Query()
            .Where(x => x.CountingCircle.SnapshotContestId == null)
            .Include(c => c.CountingCircle)
            .ThenInclude(c => c.ResponsibleAuthority)
            .OrderBy(c => c.CountingCircle.Name)
            .ToListAsync();

        return entries
            .GroupBy(x => x.DomainOfInfluenceId)
            .ToDictionary(x => x.Key, x => x.Select(g => g.CountingCircle).ToList());
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

        await Query()
            .Where(doiCc => domainOfInfluenceIds.Contains(doiCc.DomainOfInfluenceId) && countingCircleIds.Contains(doiCc.CountingCircleId))
            .ExecuteDeleteAsync();
    }
}
