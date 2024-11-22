// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class SimplePoliticalBusinessRepo : DbRepository<DataContext, SimplePoliticalBusiness>
{
    private readonly DomainOfInfluencePermissionRepo _permissionRepo;
    private readonly SimpleCountingCircleResultRepo _simpleCcResultRepo;
    private readonly DomainOfInfluenceRepo _doiRepo;

    public SimplePoliticalBusinessRepo(
        DataContext context,
        DomainOfInfluencePermissionRepo permissionRepo,
        SimpleCountingCircleResultRepo simpleCcResultRepo,
        DomainOfInfluenceRepo doiRepo)
        : base(context)
    {
        _permissionRepo = permissionRepo;
        _simpleCcResultRepo = simpleCcResultRepo;
        _doiRepo = doiRepo;
    }

    /// <summary>
    /// Gets the political businesses owned by the tenant.
    /// If the ViewCountingCirclePartialResults are set for a domain of influence which the tenant owns, then these
    /// political businesses are also returned.
    /// </summary>
    /// <param name="tenantId">Tenant id.</param>
    /// <returns>A political business queryable.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened interpolated string parameters.")]
    public IQueryable<SimplePoliticalBusiness> BuildOwnedPoliticalBusinessesQuery(string tenantId)
    {
        var permissionCcIdsColumn = _permissionRepo.GetColumnName(x => x.CountingCircleIds);
        var permissionBasisDoiIdColumn = _permissionRepo.GetColumnName(x => x.BasisDomainOfInfluenceId);
        var permissionTenantIdColumn = _permissionRepo.GetColumnName(x => x.TenantId);
        var ccResultPbIdColumn = _simpleCcResultRepo.GetColumnName(x => x.PoliticalBusinessId);
        var ccResultCcIdColumn = _simpleCcResultRepo.GetColumnName(x => x.CountingCircleId);
        var doiIdColumn = _doiRepo.GetColumnName(x => x.Id);
        var doiTenantIdColumn = _doiRepo.GetColumnName(x => x.SecureConnectId);
        var doiBasisIdColumn = _doiRepo.GetColumnName(x => x.BasisDomainOfInfluenceId);
        var doiPartialCcResultsColumn = _doiRepo.GetColumnName(x => x.ViewCountingCirclePartialResults);
        var pbIdColumn = GetDelimitedColumnName(x => x.Id);
        var pbDoiIdColumn = GetDelimitedColumnName(x => x.DomainOfInfluenceId);

        // Access to a political business is decided by the participating counting circles.
        // The tenant must either be the responsible authority of a participating counting circle
        // or the responsible authority of a domain of influence which has an assigned participating counting circle.
        return Set.FromSqlRaw(
            $@"SELECT pb.* FROM {DelimitedSchemaAndTableName} AS pb WHERE EXISTS (
                SELECT 1 FROM {_simpleCcResultRepo.DelimetedTableName} AS ccr
                WHERE ccr.{ccResultPbIdColumn} = pb.{pbIdColumn}
                AND EXISTS (
                    SELECT 1 FROM {_doiRepo.DelimetedTableName} AS doi
                    WHERE (doi.{doiIdColumn} = pb.{pbDoiIdColumn} AND doi.{doiTenantIdColumn} = {{0}})
                    OR (doi.{doiPartialCcResultsColumn} = TRUE AND EXISTS (
                        SELECT 1 FROM {_permissionRepo.DelimetedTableName} AS doip
                        WHERE {permissionTenantIdColumn} = {{0}}
                          AND doip.{permissionBasisDoiIdColumn} = doi.{doiBasisIdColumn}
                          AND ccr.{ccResultCcIdColumn} = ANY(doip.{permissionCcIdsColumn})
                        )
                    )
                )
            )",
            tenantId);
    }

    /// <summary>
    /// Gets accessible political businesses for a tenant (accessible = tenant has a domain of influence permission entry with
    /// a matching counting circle id entry).
    /// </summary>
    /// <param name="tenantId">Tenant id.</param>
    /// <returns>A political business queryable.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened interpolated string parameters.")]
    public IQueryable<SimplePoliticalBusiness> BuildAccessibleQuery(string tenantId)
    {
        var permissionCcIdsColumn = _permissionRepo.GetColumnName(x => x.CountingCircleIds);
        var permissionBasisDoiIdColumn = _permissionRepo.GetColumnName(x => x.BasisDomainOfInfluenceId);
        var permissionTenantIdColumn = _permissionRepo.GetColumnName(x => x.TenantId);
        var ccResultPbIdColumn = _simpleCcResultRepo.GetColumnName(x => x.PoliticalBusinessId);
        var ccResultCcIdColumn = _simpleCcResultRepo.GetColumnName(x => x.CountingCircleId);
        var pbIdColumn = GetDelimitedColumnName(x => x.Id);

        // Access to a political business is decided by the participating counting circles.
        // The tenant must either be the responsible authority of a participating counting circle
        // or the responsible authority of a domain of influence which has an assigned participating counting circle.
        return Set.FromSqlRaw(
            $@"WITH permission_query AS (
                SELECT ccr.{ccResultPbIdColumn}
                FROM {_permissionRepo.DelimetedTableName} dp
                JOIN {_simpleCcResultRepo.DelimetedTableName} ccr ON ccr.{ccResultCcIdColumn} = ANY(dp.{permissionCcIdsColumn})
                WHERE {permissionTenantIdColumn} = {{0}}
            )
            SELECT pb.* FROM {DelimitedSchemaAndTableName} AS pb WHERE EXISTS (
                SELECT 1 FROM permission_query WHERE permission_query.{ccResultPbIdColumn} = pb.{pbIdColumn}
            )",
            tenantId);
    }
}
