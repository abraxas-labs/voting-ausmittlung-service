// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;
using CountingCircle = Voting.Ausmittlung.Data.Models.CountingCircle;

namespace Voting.Ausmittlung.Core.Services.Permission;

public class PermissionService
{
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluencePermissionEntry> _permissionRepo;
    private readonly ILogger _logger;
    private readonly IAuth _auth;
    private readonly IAuthStore _authStore;
    private readonly AppConfig _appConfig;

    public PermissionService(
        ILogger<PermissionService> logger,
        IAuth auth,
        IAuthStore authStore,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluencePermissionEntry> permissionRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        AppConfig appConfig)
    {
        _logger = logger;
        _auth = auth;
        _countingCircleRepo = countingCircleRepo;
        _permissionRepo = permissionRepo;
        _contestRepo = contestRepo;
        _appConfig = appConfig;
        _authStore = authStore;

        ErfassungCreator = $"{appConfig.SecureConnect.AppShortNameErfassung}::Erfasser";
        ErfassungElectionAdmin = $"{appConfig.SecureConnect.AppShortNameErfassung}::Wahlverwalter";
        MonitoringElectionAdmin = $"{appConfig.SecureConnect.AppShortNameMonitoring}::Wahlverwalter";
    }

    public string ErfassungCreator { get; }

    public string ErfassungElectionAdmin { get; }

    public string MonitoringElectionAdmin { get; }

    public string UserId => _auth.User.Loginid;

    public string TenantId => _auth.Tenant.Id;

    /// <summary>
    /// Checks if the current tenant is the contest manager (meaning the current tenant is the authority of the domain of influence of the contest)
    /// and contest is in testing phase
    /// or the counting circle belongs to the current tenant (meaning the current tenant is the responsible authority of the counting circle).
    /// </summary>
    /// <param name="basisCountingCircleId">The basis counting circle ID (NOT the counting circle ID!).</param>
    /// <param name="contestId">The contest id to check.</param>
    /// <exception cref="ForbiddenException">Thrown when the current tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to the current tenant.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(Guid basisCountingCircleId, Guid contestId)
    {
        var tenantId = _auth.Tenant.Id;
        var isContestManagerAndInTestingPhase = await _contestRepo.Query()
            .AnyAsync(x => x.Id == contestId && x.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id && x.State == ContestState.TestingPhase);

        if (!isContestManagerAndInTestingPhase && !await _countingCircleRepo.Query()
                .AnyAsync(countingCircle => countingCircle.BasisCountingCircleId == basisCountingCircleId
                                            && countingCircle.SnapshotContestId == contestId
                                            && countingCircle.ResponsibleAuthority.SecureConnectId == tenantId))
        {
            throw new ForbiddenException("This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
        }
    }

    /// <summary>
    /// Checks if the current tenant is the contest manager (meaning the current tenant is the authority of the domain of influence of the contest)
    /// and contest is in testing phase
    /// or the counting circle belongs to the current tenant (meaning the current tenant is the responsible authority of the counting circle).
    /// </summary>
    /// <param name="countingCircleId">The ID of the counting circle (ID of the snapshot counting circle).</param>
    /// <param name="contestId">The contest id to check.</param>
    /// <exception cref="ForbiddenException">Thrown when the current tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to the current tenant.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircle(Guid countingCircleId, Guid contestId)
    {
        var tenantId = _auth.Tenant.Id;
        var isContestManagerAndInTestingPhase = await _contestRepo.Query()
            .AnyAsync(x => x.Id == contestId && x.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id && x.State == ContestState.TestingPhase);

        if (!isContestManagerAndInTestingPhase && !await _countingCircleRepo.Query()
                .AnyAsync(countingCircle => countingCircle.Id == countingCircleId && countingCircle.ResponsibleAuthority.SecureConnectId == tenantId))
        {
            throw new ForbiddenException("This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
        }
    }

    /// <summary>
    /// Checks if the current tenant is the contest manager (meaning the current tenant is the authority of the domain of influence of the contest)
    /// and contest is in testing phase
    /// or the counting circle belongs to the current tenant (meaning the current tenant is the responsible authority of the counting circle).
    /// </summary>
    /// <param name="countingCircle">The counting circle.</param>
    /// <param name="contest">The contest.</param>
    /// <exception cref="ForbiddenException">Thrown when the current tenant is not the contest manager and the counting circle does not belong to the current tenant.</exception>
    public void EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircle(CountingCircle countingCircle, Contest contest)
    {
        var tenantId = _auth.Tenant.Id;
        var isContestManagerAndInTestingPhase = contest.DomainOfInfluence.SecureConnectId == tenantId && !contest.TestingPhaseEnded;

        if (!isContestManagerAndInTestingPhase && countingCircle.ResponsibleAuthority.SecureConnectId != tenantId)
        {
            throw new ForbiddenException("This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
        }
    }

    /// <summary>
    /// Gets all counting circle IDs where the current tenant has read permission.
    /// </summary>
    /// <param name="contestId">The contest ID to filter the counting circles.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A set of counting circle IDs, where the current tenant has read permissions.</returns>
    public async Task<HashSet<Guid>> GetReadableCountingCircleIds(Guid contestId, CancellationToken ct = default)
    {
        var tenantId = _auth.Tenant.Id;
        var ids = await _permissionRepo.Query()
            .Where(x => x.TenantId == tenantId && x.ContestId == contestId)
            .Select(x => x.CountingCircleIds)
            .ToListAsync(ct);
        return ids.SelectMany(x => x)
            .ToHashSet();
    }

    /// <summary>
    /// Ensures that the current tenant has read permissions on the counting circle. If not, an exception is thrown.
    /// </summary>
    /// <param name="countingCircleId">The counting circle ID (ID of the snapshot counting circle) to check.</param>
    /// <param name="contestId">The contest ID.</param>
    /// <exception cref="ForbiddenException">Thrown when the current tenant has no read permissions on the counting circle.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureCanReadCountingCircle(Guid countingCircleId, Guid contestId)
    {
        var tenantId = _auth.Tenant.Id;

        var permissionOk = await _permissionRepo.Query().AnyAsync(x =>
            x.TenantId == tenantId
            && x.ContestId == contestId
            && x.CountingCircleIds.Contains(countingCircleId));

        if (!permissionOk)
        {
            throw new ForbiddenException($"no permission entries available to access {countingCircleId}");
        }
    }

    /// <summary>
    /// Checks whether the current tenant has read permissions on the counting circle.
    /// </summary>
    /// <param name="basisCountingCircleId">The basis counting circle ID (NOT the snapshot counting circle ID!) to check.</param>
    /// <param name="contestId">The contest ID.</param>
    /// <returns>Whether the current tenant has read permissions on the counting circle.</returns>
    public async Task<bool> CanReadBasisCountingCircle(Guid basisCountingCircleId, Guid contestId)
    {
        var tenantId = _auth.Tenant.Id;
        return await _permissionRepo.Query().AnyAsync(x =>
            x.TenantId == tenantId
            && x.ContestId == contestId
            && x.BasisCountingCircleIds.Contains(basisCountingCircleId));
    }

    /// <summary>
    /// Ensures that the current tenant has read permissions on the counting circle. If not, an exception is thrown.
    /// </summary>
    /// <param name="basisCountingCircleId">The basis counting circle ID (NOT the snapshot counting circle ID!) to check.</param>
    /// <param name="contestId">The contest ID.</param>
    /// <exception cref="ForbiddenException">Thrown when the current tenant has no read permissions on the counting circle.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureCanReadBasisCountingCircle(Guid basisCountingCircleId, Guid contestId)
    {
        if (!await CanReadBasisCountingCircle(basisCountingCircleId, contestId))
        {
            throw new ForbiddenException($"no permission entries available to access {basisCountingCircleId}");
        }
    }

    /// <summary>
    /// Ensures that the current tenant is the authority of the domain of influence of the contest.
    /// Also throws if the contest does not exist.
    /// </summary>
    /// <param name="contestId">The contestId.</param>
    /// <exception cref="ForbiddenException">Thrown when the contest does not exist or the current tenant is not the manager.</exception>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    public async Task EnsureIsContestManager(Guid contestId)
    {
        if (!await _contestRepo.Query().AnyAsync(x => x.Id == contestId && x.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id))
        {
            throw new ForbiddenException("no access to this contest");
        }
    }

    /// <summary>
    /// Ensures that the current tenant is the authority of the domain of influence.
    /// </summary>
    /// <param name="domainOfInfluence">The domain of influence to check.</param>
    /// <exception cref="ForbiddenException">Thrown when the current tenant is not the manager.</exception>
    public void EnsureIsDomainOfInfluenceManager(DomainOfInfluence domainOfInfluence)
    {
        if (!domainOfInfluence.SecureConnectId.Equals(_auth.Tenant.Id, StringComparison.Ordinal))
        {
            throw new ForbiddenException("no access to this domain of influence");
        }
    }

    /// <summary>
    /// Checks whether the current tenant has read permissions on the contest. If not, an exception is thrown.
    /// </summary>
    /// <param name="contestId">The contest ID to check.</param>
    /// <exception cref="ForbiddenException">Thrown when the current tenant has no read permissions on the contest.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureCanReadContest(Guid contestId)
    {
        var canAccessContest = await _permissionRepo
            .Query()
            .AnyAsync(p => p.TenantId == _auth.Tenant.Id && p.ContestId == contestId);

        if (!canAccessContest)
        {
            throw new ForbiddenException($"you have no read access on contest {contestId}");
        }
    }

    /// <summary>
    /// Checks whether the current user has enough permissions to export a result.
    /// </summary>
    /// <param name="type">The type of the result to export..</param>
    /// <exception cref="ForbiddenException">Thrown when the current user does not have enough permissions to export the result.</exception>
    public void EnsureCanExport(ResultType type)
    {
        switch (type)
        {
            case ResultType.CountingCircleResult:
            case ResultType.MultiplePoliticalBusinessesCountingCircleResult:
            case ResultType.PoliticalBusinessResultBundleReview:
                EnsureAnyRole();
                break;
            case ResultType.MultiplePoliticalBusinessesResult:
            case ResultType.PoliticalBusinessResult:
            case ResultType.PoliticalBusinessUnionResult:
            case ResultType.Contest:
                EnsureMonitoringElectionAdmin();
                break;
            default:
                throw new ForbiddenException();
        }
    }

    public bool IsErfassungElectionAdmin()
        => _auth.HasRole(ErfassungElectionAdmin);

    public bool IsMonitoringElectionAdmin()
        => _auth.HasRole(MonitoringElectionAdmin);

    /// <summary>
    /// Checks whether the current user has any role at all. Throws if the user does not have a role.
    /// </summary>
    /// <exception cref="ForbiddenException">Thrown if the current user does not have a role.</exception>
    public void EnsureAnyRole()
        => _auth.EnsureAnyRole(ErfassungCreator, ErfassungElectionAdmin, MonitoringElectionAdmin);

    public void EnsureMonitoringElectionAdmin()
        => _auth.EnsureRole(MonitoringElectionAdmin);

    public void EnsureErfassungElectionAdmin()
        => _auth.EnsureRole(ErfassungElectionAdmin);

    public void EnsureErfassungElectionAdminOrMonitoringElectionAdmin()
        => _auth.EnsureAnyRole(ErfassungElectionAdmin, MonitoringElectionAdmin);

    public void EnsureErfassungElectionAdminOrCreator()
        => _auth.EnsureAnyRole(ErfassungElectionAdmin, ErfassungCreator);

    /// <summary>
    /// Ensures that the current tenant is the authority of the domain of influence of the contest.
    /// </summary>
    /// <param name="contest">The contest.</param>
    /// <exception cref="ForbiddenException">Thrown when the current tenant is not the manager.</exception>
    internal void EnsureIsContestManager(Contest contest)
    {
        if (!_auth.Tenant.Id.Equals(contest.DomainOfInfluence.SecureConnectId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("no access to this contest");
        }
    }

    /// <summary>
    /// Sets the configured Abraxas service user authentication if no authentication is currently provided.
    /// This should only be used for background jobs or similar things.
    /// </summary>
    internal void SetAbraxasAuthIfNotAuthenticated()
    {
        if (!_auth.IsAuthenticated)
        {
            _logger.LogInformation(SecurityLogging.SecurityEventId, "Using Abraxas authentication values, since no user is authenticated");
            _authStore.SetValues(new() { Loginid = _appConfig.SecureConnect.ServiceUserId }, new() { Id = _appConfig.SecureConnect.AbraxasTenantId }, Enumerable.Empty<string>());
        }
    }
}
