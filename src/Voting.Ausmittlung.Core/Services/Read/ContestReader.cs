// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ContestReader
{
    private readonly IAuth _auth;
    private readonly IDbRepository<DataContext, Contest> _repo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _proportionalElectionUnionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionUnion> _majorityElectionUnionRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluenceCountingCircle> _domainOfInfluenceCountingCircleRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly SimplePoliticalBusinessRepo _simplePoliticalBusinessRepo;
    private readonly PermissionService _permissionService;

    public ContestReader(
        IAuth auth,
        IDbRepository<DataContext, Contest> repo,
        IDbRepository<DataContext, ProportionalElectionUnion> proportionalElectionUnionRepo,
        IDbRepository<DataContext, MajorityElectionUnion> majorityElectionUnionRepo,
        IDbRepository<DataContext, DomainOfInfluenceCountingCircle> domainOfInfluenceCountingCircleRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        SimplePoliticalBusinessRepo simplePoliticalBusinessRepo,
        PermissionService permissionService)
    {
        _auth = auth;
        _repo = repo;
        _permissionService = permissionService;
        _proportionalElectionUnionRepo = proportionalElectionUnionRepo;
        _majorityElectionUnionRepo = majorityElectionUnionRepo;
        _domainOfInfluenceCountingCircleRepo = domainOfInfluenceCountingCircleRepo;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
        _simpleResultRepo = simpleResultRepo;
    }

    public async Task<Contest> Get(Guid id)
    {
        var countingCircleIds = await _permissionService.GetReadableCountingCircleIds(id);

        var query = _repo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.CantonDefaults)
            .Where(x => x.SimplePoliticalBusinesses.Any(pb =>
                pb.Active
                && pb.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection
                && pb.SimpleResults.Any(cc => countingCircleIds.Contains(cc.CountingCircleId))));

        if (_auth.HasPermission(Permissions.PoliticalBusiness.ReadOwned))
        {
            var viewablePartialResultsCcIds = await _permissionService.GetViewablePartialResultsCountingCircleIds(id);
            query = query
                .Where(x => x.SimplePoliticalBusinesses.Any(pb =>
                    pb.DomainOfInfluence.SecureConnectId == _permissionService.TenantId
                    || pb.SimpleResults.Any(cc => viewablePartialResultsCcIds.Contains(cc.CountingCircleId))));
        }

        return await query
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    public async Task<List<CountingCircle>> GetAccessibleCountingCircles(Guid contestId)
    {
        var readableCountingCircleIds = await _permissionService.GetReadableCountingCircleIds(contestId);

        var canReadOwnedPbs = _auth.HasPermission(Permissions.PoliticalBusiness.ReadOwned);
        var tenantId = _auth.Tenant.Id;

        var contest = await _repo.Query()
            .Include(x => x.DomainOfInfluence)
            .SingleAsync(x => x.Id == contestId);

        var viewablePartialResultsCountingCircleIds = await _permissionService.GetViewablePartialResultsCountingCircleIds(contestId);

        // counting circle ids which is the current tenant responsible or contest manager or user have access to read owned political businesses
        var responsibleCountingCircleIds = await _simpleResultRepo.Query()
            .Include(x => x.PoliticalBusiness!.DomainOfInfluence)
            .Include(x => x.CountingCircle!.ResponsibleAuthority)
            .Where(x =>
                x.CountingCircle!.ResponsibleAuthority.SecureConnectId == tenantId ||
                contest.DomainOfInfluence.SecureConnectId == tenantId ||
                (canReadOwnedPbs && x.PoliticalBusiness!.DomainOfInfluence.SecureConnectId == tenantId))
            .Select(x => x.CountingCircleId)
            .Distinct()
            .ToListAsync();

        var accessibleCountingCircleIds = !canReadOwnedPbs
            ? readableCountingCircleIds.Intersect(responsibleCountingCircleIds)
            : readableCountingCircleIds.Intersect(responsibleCountingCircleIds.Concat(viewablePartialResultsCountingCircleIds));

        var doiCcs = await _domainOfInfluenceCountingCircleRepo.Query()
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.ResponsibleAuthority)
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.ContactPersonDuringEvent)
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.ContactPersonAfterEvent)
            .Where(x => accessibleCountingCircleIds.Contains(x.CountingCircleId) && x.DomainOfInfluenceId == contest.DomainOfInfluenceId)
            .ToListAsync();

        return doiCcs.DistinctBy(x => x.CountingCircleId)
            .Select(x => x.CountingCircle)
            .OrderBy(x => x.Name)
            .ToList();
    }

    public async Task<List<ContestSummary>> ListSummaries(IReadOnlyCollection<ContestState> states)
    {
        var tenantId = _permissionService.TenantId;

        // Careful! ListSummaries lists all accessible contests that were ever created.
        // To find out which contests are accessible, we need to access the counting circles, domain of influences and permissions.
        // This may be a huge number of entities, since they will grow with each created contest (due to snapshotting).
        // When refactoring or changing this query, use a backup from an environment with a huge number of contests.
        var query = _auth.HasPermission(Permissions.PoliticalBusiness.ReadOwned)
            ? _simplePoliticalBusinessRepo.BuildOwnedPoliticalBusinessesQuery(tenantId)
            : _simplePoliticalBusinessRepo.BuildAccessibleQuery(tenantId);

        query = query.Where(pb => pb.Active && pb.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection);

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
            .Include(c => c.CantonDefaults)
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
        var tenantId = _permissionService.TenantId;

        var proportionalElectionUnions = await _proportionalElectionUnionRepo.Query()
            .Include(x => x.ProportionalElectionUnionEntries)
            .ThenInclude(x => x.ProportionalElection)
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

    public async Task<ContestCantonDefaults> GetCantonDefaults(Guid contestId)
    {
        var contest = await Get(contestId);
        return contest.CantonDefaults;
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
            .Include(x => x.Contest.CantonDefaults)
            .Include(x => x.SimpleResults)
            .ToListAsync();
    }

    internal async Task<List<SimplePoliticalBusiness>> GetOwnedPoliticalBusinesses(Guid contestId)
    {
        var viewablePartialResultsCcIds = await _permissionService.GetViewablePartialResultsCountingCircleIds(contestId);
        return await _repo.Query()
            .AsSingleQuery()
            .Where(x => x.Id == contestId)
            .SelectMany(x => x.SimplePoliticalBusinesses)
            .Where(x => x.Active && (x.DomainOfInfluence.SecureConnectId == _permissionService.TenantId || x.SimpleResults.Any(cc => viewablePartialResultsCcIds.Contains(cc.CountingCircleId))))
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ThenBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessType)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Translations)
            .Include(x => x.Contest.CantonDefaults)
            .Include(x => x.SimpleResults)
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
