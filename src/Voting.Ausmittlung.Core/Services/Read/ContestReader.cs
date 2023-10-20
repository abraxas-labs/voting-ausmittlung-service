// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ContestReader
{
    private readonly IDbRepository<DataContext, Contest> _repo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _proportionalElectionUnionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionUnion> _majorityElectionUnionRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluenceCountingCircle> _domainOfInfluenceCountingCircleRepo;
    private readonly SimplePoliticalBusinessRepo _simplePoliticalBusinessRepo;
    private readonly PermissionService _permissionService;

    public ContestReader(
        IDbRepository<DataContext, Contest> repo,
        IDbRepository<DataContext, ProportionalElectionUnion> proportionalElectionUnionRepo,
        IDbRepository<DataContext, MajorityElectionUnion> majorityElectionUnionRepo,
        IDbRepository<DataContext, DomainOfInfluenceCountingCircle> domainOfInfluenceCountingCircleRepo,
        SimplePoliticalBusinessRepo simplePoliticalBusinessRepo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
        _proportionalElectionUnionRepo = proportionalElectionUnionRepo;
        _majorityElectionUnionRepo = majorityElectionUnionRepo;
        _domainOfInfluenceCountingCircleRepo = domainOfInfluenceCountingCircleRepo;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
    }

    public async Task<Contest> Get(Guid id)
    {
        _permissionService.EnsureAnyRole();

        var countingCircleIds = await _permissionService.GetReadableCountingCircleIds(id);

        var isMonitoringAdmin = _permissionService.IsMonitoringElectionAdmin();
        return await _repo.Query()
                   .AsSplitQuery()
                   .Include(x => x.Translations)
                   .Include(x => x.DomainOfInfluence)
                   .Where(x => x.SimplePoliticalBusinesses.Any(pb =>
                       pb.Active
                       && pb.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection
                       && (!isMonitoringAdmin || pb.DomainOfInfluence.SecureConnectId == _permissionService.TenantId)
                       && pb.SimpleResults.Any(cc => countingCircleIds.Contains(cc.CountingCircleId))))
                   .FirstOrDefaultAsync(c => c.Id == id)
               ?? throw new EntityNotFoundException(id);
    }

    public async Task<List<CountingCircle>> GetAccessibleCountingCircles(Guid contestId)
    {
        _permissionService.EnsureAnyRole();

        var countingCircleIds = await _permissionService.GetReadableCountingCircleIds(contestId);

        var contest = await _repo.GetByKey(contestId)
                      ?? throw new EntityNotFoundException(contestId);

        return await _domainOfInfluenceCountingCircleRepo.Query()
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.ResponsibleAuthority)
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.ContactPersonDuringEvent)
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.ContactPersonAfterEvent)
            .Where(x => countingCircleIds.Contains(x.CountingCircleId) && x.DomainOfInfluenceId == contest.DomainOfInfluenceId)
            .Select(x => x.CountingCircle)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<List<ContestSummary>> ListSummaries(IReadOnlyCollection<ContestState> states)
    {
        _permissionService.EnsureAnyRole();
        var tenantId = _permissionService.TenantId;

        // Careful! ListSummaries lists all accessible contests that were ever created.
        // To find out which contests are accessible, we need to access the counting circles,
        // which may be a huge number of entities, since they will grow with each created contest (due to snapshotting).
        // Keep this in mind when refactoring this.
        var query = _simplePoliticalBusinessRepo.BuildAccessibleQuery(tenantId)
            .Where(pb => pb.Active && pb.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection);

        if (_permissionService.IsMonitoringElectionAdmin())
        {
            query = query.Where(pb => pb.DomainOfInfluence.SecureConnectId == tenantId);
        }

        if (states.Count > 0)
        {
            query = query.Where(pb => states.Contains(pb.Contest.State));
        }

        var contestCounts = await query
            .GroupBy(x => new { x.ContestId, DoiType = x.DomainOfInfluence.Type })
            .Select(x => new { x.Key.ContestId, x.Key.DoiType, Count = x.Count() })
            .ToListAsync();
        var countsByContestId = contestCounts
            .GroupBy(x => x.ContestId)
            .ToDictionary(x => x.Key);

        // This does an IN query with all accessible contest IDs. Should be a manageable amount of IDs,
        // since only a few contests are created per year.
        var summaries = await _repo.Query()
            .AsSplitQuery()
            .Include(c => c.DomainOfInfluence)
            .Include(c => c.Translations)
            .Where(c => countsByContestId.Keys.Contains(c.Id))
            .Order(states)
            .Select(c => new ContestSummary { Contest = c })
            .ToListAsync();

        foreach (var summary in summaries)
        {
            var counts = countsByContestId[summary.Contest.Id];
            summary.ContestEntriesDetails = counts
                .OrderBy(x => x.DoiType)
                .Select(c => new ContestSummaryEntryDetails
                {
                    DomainOfInfluenceType = c.DoiType,
                    ContestEntriesCount = c.Count,
                })
                .ToList();
        }

        return summaries;
    }

    public async Task<IEnumerable<PoliticalBusinessUnion>> ListPoliticalBusinessUnions(Guid contestId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();

        var tenantId = _permissionService.TenantId;

        var proportionalElectionUnions = await _proportionalElectionUnionRepo.Query()
            .Where(x =>
                x.ContestId == contestId
                && x.SecureConnectId == tenantId)
            .ToListAsync();

        var majorityElectionUnions = await _majorityElectionUnionRepo.Query()
            .Where(x =>
                x.ContestId == contestId
                && x.SecureConnectId == tenantId)
            .ToListAsync();

        return proportionalElectionUnions
            .Cast<PoliticalBusinessUnion>()
            .Union(majorityElectionUnions)
            .OrderBy(x => x.Description);
    }

    internal async Task<IReadOnlySet<Guid>> GetAccessiblePoliticalBusinessIds(Guid contestId)
    {
        var countingCircleIds = await _permissionService.GetReadableCountingCircleIds(contestId);
        var ids = await _repo.Query()
            .Where(x => x.Id == contestId)
            .SelectMany(x => x.SimplePoliticalBusinesses)
            .Where(x => x.Active && x.SimpleResults.Any(cc => countingCircleIds.Contains(cc.CountingCircleId)))
            .Select(x => x.Id)
            .ToListAsync();
        return ids.ToHashSet();
    }

    internal async Task<List<SimplePoliticalBusiness>> GetAccessiblePoliticalBusinesses(Guid basisCountingCircleId, Guid contestId)
    {
        if (!await _permissionService.CanReadBasisCountingCircle(basisCountingCircleId, contestId))
        {
            return new();
        }

        return await _repo.Query()
            .AsSingleQuery()
            .Where(x => x.Id == contestId)
            .SelectMany(x => x.SimplePoliticalBusinesses)
            .Where(x => x.Active && x.SimpleResults.Any(cc => cc.CountingCircle!.BasisCountingCircleId == basisCountingCircleId))
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ThenBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessType)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Translations)
            .ToListAsync();
    }

    internal async Task<List<SimplePoliticalBusiness>> GetOwnedPoliticalBusinesses(Guid contestId)
    {
        return await _repo.Query()
            .AsSingleQuery()
            .Where(x => x.Id == contestId)
            .SelectMany(x => x.SimplePoliticalBusinesses)
            .Where(x => x.Active && x.DomainOfInfluence.SecureConnectId == _permissionService.TenantId)
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ThenBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessType)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Translations)
            .ToListAsync();
    }

    internal async Task<IReadOnlySet<Guid>> GetOwnedPoliticalBusinessIds(Guid contestId)
    {
        var ids = await _repo.Query()
            .Where(x => x.Id == contestId)
            .SelectMany(x => x.SimplePoliticalBusinesses)
            .Where(x => x.Active && x.DomainOfInfluence.SecureConnectId == _permissionService.TenantId)
            .Select(x => x.Id)
            .ToListAsync();
        return ids.ToHashSet();
    }
}
