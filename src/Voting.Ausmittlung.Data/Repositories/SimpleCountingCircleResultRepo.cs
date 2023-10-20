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

public class SimpleCountingCircleResultRepo : DbRepository<DataContext, SimpleCountingCircleResult>
{
    public SimpleCountingCircleResultRepo(DataContext context)
        : base(context)
    {
    }

    internal string DelimetedTableName => DelimitedSchemaAndTableName;

    public async Task Reset(Guid contestId)
    {
        var results = await Set
            .Where(cc => cc.PoliticalBusiness!.ContestId == contestId)
            .Select(x => new
            {
                x.Id,
                x.CountingCircleId,
                x.PoliticalBusinessId,
                x.CountingCircle!.BasisCountingCircleId,
            })
            .ToListAsync();
        await DeleteRangeByKey(results.Select(x => x.Id));
        await CreateRange(results.Select(x => new SimpleCountingCircleResult
        {
            Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(x.PoliticalBusinessId, x.BasisCountingCircleId, true),
            CountingCircleId = x.CountingCircleId,
            PoliticalBusinessId = x.PoliticalBusinessId,
        }));
    }

    public async Task Sync(Guid politicalBusinessId, Guid domainOfInfluenceId, bool testingPhaseEnded)
    {
        var countingCircles = await Context.Set<DomainOfInfluenceCountingCircle>()
            .Where(cc => cc.DomainOfInfluenceId == domainOfInfluenceId)
            .Select(cc => new { cc.CountingCircleId, cc.CountingCircle.BasisCountingCircleId })
            .ToListAsync();
        var basisCountingCircleIdByContestCountingCircleId = countingCircles.ToDictionary(
            x => x.CountingCircleId,
            x => x.BasisCountingCircleId);
        var contestCountingCircleIds = countingCircles.ConvertAll(x => x.CountingCircleId);

        var existingEntries = await Set
            .Where(x => x.PoliticalBusinessId == politicalBusinessId)
            .ToListAsync();

        var entityIdsToRemove = existingEntries
            .Where(e => !contestCountingCircleIds.Contains(e.CountingCircleId))
            .Select(e => e.Id)
            .ToList();
        await DeleteRangeByKey(entityIdsToRemove);

        var entriesToCreate = contestCountingCircleIds.Except(existingEntries.Select(x => x.CountingCircleId))
            .Select(cid => new SimpleCountingCircleResult
            {
                Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(politicalBusinessId, basisCountingCircleIdByContestCountingCircleId[cid], testingPhaseEnded),
                CountingCircleId = cid,
                PoliticalBusinessId = politicalBusinessId,
            });

        Set.AddRange(entriesToCreate);
        await Context.SaveChangesAsync();
    }

    internal string GetColumnName<TProp>(Expression<Func<SimpleCountingCircleResult, TProp>> memberAccess)
        => GetDelimitedColumnName(memberAccess);
}
