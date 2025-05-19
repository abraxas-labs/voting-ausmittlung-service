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
        // if adjusted, ensure testing and analyzing query (plans)
        return Set.FromSqlRaw(
            $$"""
              WITH EligiblePoliticalBusinessIds AS (
                SELECT pb.{{pbIdColumn}}
                FROM {{DelimitedSchemaAndTableName}} AS pb
                INNER JOIN {{_simpleCcResultRepo.DelimetedTableName}} AS ccr
                    ON ccr.{{ccResultPbIdColumn}} = pb.{{pbIdColumn}}
                INNER JOIN {{_doiRepo.DelimetedTableName}} AS doi
                    ON doi.{{doiIdColumn}} = pb.{{pbDoiIdColumn}}
                WHERE doi.{{doiTenantIdColumn}} = {0}

                UNION

                SELECT pb.{{pbIdColumn}}
                FROM {{DelimitedSchemaAndTableName}} AS pb
                INNER JOIN {{_simpleCcResultRepo.DelimetedTableName}} AS ccr
                    ON ccr.{{ccResultPbIdColumn}} = pb.{{pbIdColumn}}
                INNER JOIN {{_permissionRepo.DelimetedTableName}} AS doip
                    ON doip.{{permissionTenantIdColumn}} = {0}
                    AND ARRAY[ccr.{{ccResultCcIdColumn}}] <@ (doip.{{permissionCcIdsColumn}})
                INNER JOIN {{_doiRepo.DelimetedTableName}} AS doi
                    ON doi.{{doiPartialCcResultsColumn}} = TRUE
                    AND doi.{{doiBasisIdColumn}} = doip.{{permissionBasisDoiIdColumn}}
              )
              SELECT pb.*
              FROM {{DelimitedSchemaAndTableName}} AS pb
              INNER JOIN EligiblePoliticalBusinessIds AS eligible ON pb.{{pbIdColumn}} = eligible.{{pbIdColumn}}
              """,
            tenantId);
    }

    /// <summary>
    /// Gets accessible political businesses for a tenant (accessible = tenant is responsible for counting the result).
    /// During the testing phase, the contest owner also has access.
    /// </summary>
    /// <param name="tenantId">Tenant id.</param>
    /// <returns>A political business queryable.</returns>
    public IQueryable<SimplePoliticalBusiness> BuildAccessibleQuery(string tenantId)
    {
        return Query()
            .Where(pb => pb.SimpleResults.Any(ccr => ccr.CountingCircle!.ResponsibleAuthority.SecureConnectId == tenantId)
                || (pb.Contest.DomainOfInfluence.SecureConnectId == tenantId && pb.Contest.State <= ContestState.TestingPhase));
    }
}
