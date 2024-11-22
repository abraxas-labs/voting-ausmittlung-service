// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Extensions;
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

    public async Task Rebuild(Guid politicalBusinessId, Guid domainOfInfluenceId, bool testingPhaseEnded, Guid contestId)
    {
        var countingCircles = await Context.Set<DomainOfInfluenceCountingCircle>()
            .Include(x => x.CountingCircle)
            .Where(cc => cc.DomainOfInfluenceId == domainOfInfluenceId)
            .ToListAsync();

        var countingCirclesWithBasisCcId = countingCircles
            .DistinctBy(x => x.CountingCircleId)
            .Select(cc => new { cc.CountingCircleId, cc.CountingCircle.BasisCountingCircleId })
            .ToList();

        var basisCountingCircleIdByContestCountingCircleId = countingCirclesWithBasisCcId.ToDictionary(
            x => x.CountingCircleId,
            x => x.BasisCountingCircleId);
        var contestCountingCircleIds = countingCirclesWithBasisCcId.ConvertAll(x => x.CountingCircleId);

        // only navigation properties can be used by ef.
        var filter = FilterByPoliticalBusinessId(politicalBusinessId);

        var existingResults = await Set
            .Where(filter)
            .Select(x => new { x.Id, x.CountingCircleId })
            .ToListAsync();

        var entityIdsToRemove = existingResults
            .Where(e => !contestCountingCircleIds.Contains(e.CountingCircleId))
            .Select(e => e.Id)
            .ToList();
        await DeleteRangeByKey(entityIdsToRemove);

        var toCreate = contestCountingCircleIds
            .Except(existingResults.Select(e => e.CountingCircleId))
            .ToList();
        var ccDetailsByCountingCircleId = await Context.Set<ContestCountingCircleDetails>()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Where(x => x.ContestId == contestId && toCreate.Contains(x.CountingCircleId))
            .ToDictionaryAsync(x => x.CountingCircleId);

        var politicalBusiness = await LoadPoliticalBusiness(politicalBusinessId);

        var entriesToCreate = toCreate
            .Select(cid =>
            {
                var totalCountOfVoters = 0;
                if (ccDetailsByCountingCircleId.TryGetValue(cid, out var details))
                {
                    totalCountOfVoters = details.GetTotalCountOfVotersForDomainOfInfluence(politicalBusiness.DomainOfInfluence);
                }

                return new T
                {
                    Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(politicalBusinessId, basisCountingCircleIdByContestCountingCircleId[cid], testingPhaseEnded),
                    CountingCircleId = cid,
                    PoliticalBusinessId = politicalBusinessId,
                    TotalCountOfVoters = totalCountOfVoters,
                };
            });

        Set.AddRange(entriesToCreate);
        await Context.SaveChangesAsync();
    }

    protected abstract Expression<Func<T, bool>> FilterByPoliticalBusinessId(Guid id);

    protected abstract Task<PoliticalBusiness> LoadPoliticalBusiness(Guid id);
}
