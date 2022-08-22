// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class DomainOfInfluencePermissionBuilder
{
    private readonly IDbRepository<DataContext, DomainOfInfluence> _repo;
    private readonly DomainOfInfluencePermissionRepo _permissionsRepo;
    private readonly DomainOfInfluenceCountingCircleRepo _countingCircleRepo;

    public DomainOfInfluencePermissionBuilder(
        IDbRepository<DataContext, DomainOfInfluence> repo,
        DomainOfInfluenceCountingCircleRepo countingCircleRepo,
        DomainOfInfluencePermissionRepo permissionsRepo)
    {
        _repo = repo;
        _countingCircleRepo = countingCircleRepo;
        _permissionsRepo = permissionsRepo;
    }

    internal async Task RebuildPermissionTree()
    {
        var allDomainOfInfluences = await _repo.Query()
            .WhereContestIsInTestingPhase()
            .ToListAsync();
        var countingCirclesByDomainOfInfluenceId = await _countingCircleRepo.CountingCirclesByDomainOfInfluenceId();
        var tree = DomainOfInfluenceTreeBuilder.BuildTree(allDomainOfInfluences, countingCirclesByDomainOfInfluenceId);
        var allTenantIds = allDomainOfInfluences
            .Select(d => d.SecureConnectId)
            .Union(countingCirclesByDomainOfInfluenceId.Values.SelectMany(c =>
                c.Select(cc => cc.CountingCircle.ResponsibleAuthority.SecureConnectId)))
            .Distinct();
        var permissions = allTenantIds.SelectMany(tid => BuildEntriesForTenant(tree, tid)).ToList();
        await _permissionsRepo.Replace(permissions);
    }

    internal Task SetContestPermissionsFinal(Guid contestId)
    {
        return _permissionsRepo.SetContestPermissionsFinal(contestId);
    }

    private IEnumerable<DomainOfInfluencePermissionEntry> BuildEntriesForTenant(
        IEnumerable<DomainOfInfluence> entries,
        string tenantId)
    {
        var tenantEntries =
            new Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry>();
        BuildEntriesForTenant(entries, tenantId, tenantEntries);
        return tenantEntries.Values;
    }

    private void BuildEntriesForTenant(
        IEnumerable<DomainOfInfluence> entries,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries,
        bool hasAccessToParent = false)
    {
        foreach (var entry in entries)
        {
            BuildEntriesForTenant(entry, tenantId, permissionEntries, hasAccessToParent);
        }
    }

    private void BuildEntriesForTenant(
        DomainOfInfluence doi,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries,
        bool hasAccessToParent = false)
    {
        var hasDirectAccess = doi.SecureConnectId == tenantId || hasAccessToParent;
        var filteredCountingCircles = doi.CountingCircles
            .Where(c => hasDirectAccess || c.CountingCircle.ResponsibleAuthority.SecureConnectId == tenantId)
            .ToList();

        if (hasDirectAccess || filteredCountingCircles.Count > 0)
        {
            var entry = new DomainOfInfluencePermissionEntry
            {
                IsParent = !hasDirectAccess,
                TenantId = tenantId,
                BasisDomainOfInfluenceId = doi.BasisDomainOfInfluenceId,
                BasisCountingCircleIds = filteredCountingCircles.Select(c => c.CountingCircle.BasisCountingCircleId).ToList(),
                CountingCircleIds = filteredCountingCircles.Select(c => c.CountingCircle.Id).ToList(),
                ContestId = doi.SnapshotContestId!.Value,
            };

            permissionEntries[(tenantId, doi.Id)] = entry;

            AddParentsToPermissions(doi, tenantId, permissionEntries);
        }

        BuildEntriesForTenant(doi.Children, tenantId, permissionEntries, hasDirectAccess);
    }

    private void AddParentsToPermissions(
        DomainOfInfluence doi,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries)
    {
        var currentParent = doi.Parent;
        while (currentParent != null)
        {
            var key = (tenantId, currentParent.Id);
            if (permissionEntries.ContainsKey(key))
            {
                return;
            }

            permissionEntries[key] =
                new DomainOfInfluencePermissionEntry
                {
                    IsParent = true,
                    TenantId = tenantId,
                    BasisDomainOfInfluenceId = currentParent.BasisDomainOfInfluenceId,
                    ContestId = doi.SnapshotContestId!.Value,
                };
            currentParent = currentParent.Parent;
        }
    }
}
