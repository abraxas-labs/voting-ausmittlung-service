// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public abstract class PoliticalBusinessResultRepo<T> : DbRepository<DataContext, T>
    where T : CountingCircleResult, new()
{
    protected PoliticalBusinessResultRepo(DataContext context)
        : base(context)
    {
    }

    public async Task Rebuild(Guid politicalBusinessId, Guid domainOfInfluenceId, bool testingPhaseEnded)
    {
        var countingCircles = await Context.Set<DomainOfInfluenceCountingCircle>()
            .Where(cc => cc.DomainOfInfluenceId == domainOfInfluenceId)
            .Select(cc => new { cc.CountingCircleId, cc.CountingCircle.BasisCountingCircleId })
            .ToListAsync();
        var basisCountingCircleIdByContestCountingCircleId = countingCircles.ToDictionary(
            x => x.CountingCircleId,
            x => x.BasisCountingCircleId);
        var contestCountingCircleIds = countingCircles.ConvertAll(x => x.CountingCircleId);

        // only navigation properties can be used by ef.
        var filter = FilterByPoliticalBusinessId(politicalBusinessId);

        var existingEntries = await Set
            .Where(filter)
            .ToListAsync();

        var entityIdsToRemove = existingEntries
            .Where(e => !contestCountingCircleIds.Contains(e.CountingCircleId))
            .Select(e => e.Id)
            .ToList();
        await DeleteRangeByKey(entityIdsToRemove);

        var entriesToCreate = contestCountingCircleIds.Except(existingEntries.Select(x => x.CountingCircleId))
            .Select(cid => new T
            {
                Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(politicalBusinessId, basisCountingCircleIdByContestCountingCircleId[cid], testingPhaseEnded),
                CountingCircleId = cid,
                PoliticalBusinessId = politicalBusinessId,
            });

        Set.AddRange(entriesToCreate);
        await Context.SaveChangesAsync();
    }

    protected abstract Expression<Func<T, bool>> FilterByPoliticalBusinessId(Guid id);
}
