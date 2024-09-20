// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class DomainOfInfluencePermissionBuilder
{
    private readonly IDbRepository<DataContext, DomainOfInfluence> _repo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly DomainOfInfluencePermissionRepo _permissionsRepo;
    private readonly DomainOfInfluenceCountingCircleRepo _countingCircleRepo;

    public DomainOfInfluencePermissionBuilder(
        IDbRepository<DataContext, DomainOfInfluence> repo,
        IDbRepository<DataContext, Contest> contestRepo,
        DomainOfInfluenceCountingCircleRepo countingCircleRepo,
        DomainOfInfluencePermissionRepo permissionsRepo)
    {
        _repo = repo;
        _contestRepo = contestRepo;
        _countingCircleRepo = countingCircleRepo;
        _permissionsRepo = permissionsRepo;
    }

    internal async Task RebuildPermissionTree()
    {
        var contestsInTestingPhase = await _contestRepo.Query()
            .WhereInTestingPhase()
            .Select(x => new
            {
                x.Id,
                BasisDoiId = x.DomainOfInfluence.BasisDomainOfInfluenceId,
            })
            .ToListAsync();

        if (contestsInTestingPhase.Count == 0)
        {
            // No need to update anything
            return;
        }

        // Since building the permission tree is complex and depends on a lot of data, we cannot partially update it (ex. when a domain of influence is deleted).
        // The idea is to fetch all domain of influences that currently exist. Those are the "same" for all contests in the testing phase.
        // Then we pick the root domain of influence for each contest and build the permission for that.
        var basisDomainOfInfluences = await _repo.Query()
            .Where(x => x.SnapshotContestId == null)
            .ToListAsync();
        var basisCountingCirclesByDoiId = await _countingCircleRepo.BasisCountingCirclesByDomainOfInfluenceId();

        var basisTree = DomainOfInfluenceTreeBuilder.BuildTree(basisDomainOfInfluences);
        var allTenantIds = basisDomainOfInfluences
            .Select(d => d.SecureConnectId)
            .Union(basisCountingCirclesByDoiId.Values.SelectMany(x => x.Select(cc => cc.ResponsibleAuthority.SecureConnectId)))
            .Distinct()
            .ToList();

        var permissions = contestsInTestingPhase
            .SelectMany(x => BuildEntriesForContest(x.Id, x.BasisDoiId, basisTree, allTenantIds, basisCountingCirclesByDoiId))
            .ToList();
        await _permissionsRepo.Replace(permissions);
    }

    internal async Task SetContestPermissionsFinal(Guid contestId)
    {
        await _permissionsRepo.Query()
            .Where(x => x.ContestId == contestId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsFinal, true));
    }

    private IEnumerable<DomainOfInfluencePermissionEntry> BuildEntriesForContest(
        Guid contestId,
        Guid contestBasisDoiId,
        List<DomainOfInfluence> entries,
        List<string> tenantIds,
        Dictionary<Guid, List<CountingCircle>> basisCountingCirclesByDoiId)
    {
        var contestDoiRoot = entries.Single(x => HasMatchingId(x, contestBasisDoiId));
        return tenantIds.SelectMany(tenantId =>
            BuildEntriesForTenant(contestDoiRoot, tenantId, contestId, basisCountingCirclesByDoiId));
    }

    private bool HasMatchingId(DomainOfInfluence doi, Guid id)
    {
        return doi.Id == id || doi.Children.Any(x => HasMatchingId(x, id));
    }

    private IEnumerable<DomainOfInfluencePermissionEntry> BuildEntriesForTenant(
        DomainOfInfluence doi,
        string tenantId,
        Guid contestId,
        Dictionary<Guid, List<CountingCircle>> basisCountingCirclesByDoiId,
        bool hasAccessToParent = false)
    {
        var hasDirectAccess = doi.SecureConnectId == tenantId || hasAccessToParent;
        var filteredCountingCircles = basisCountingCirclesByDoiId.GetValueOrDefault(doi.Id, new List<CountingCircle>())
            .Where(c => hasDirectAccess || c.ResponsibleAuthority.SecureConnectId == tenantId)
            .ToList();

        if (hasDirectAccess || filteredCountingCircles.Count > 0)
        {
            yield return new DomainOfInfluencePermissionEntry
            {
                IsParent = !hasDirectAccess,
                TenantId = tenantId,
                BasisDomainOfInfluenceId = doi.Id,
                BasisCountingCircleIds = filteredCountingCircles.ConvertAll(c => c.Id),
                CountingCircleIds = filteredCountingCircles.ConvertAll(c => AusmittlungUuidV5.BuildCountingCircleSnapshot(contestId, c.Id)),
                ContestId = contestId,
            };
        }

        foreach (var child in doi.Children)
        {
            foreach (var permission in BuildEntriesForTenant(child, tenantId, contestId, basisCountingCirclesByDoiId, hasDirectAccess))
            {
                yield return permission;
            }
        }
    }
}
