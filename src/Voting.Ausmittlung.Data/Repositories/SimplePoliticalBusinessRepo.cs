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

        // If adjusted, ensure testing and analyzing query (plans)
        // Use a big dataset for testing and test the ViewCountingCirclePartialResults performance
        return Set.FromSqlRaw(
            $$"""
              SELECT *
              FROM {{DelimitedSchemaAndTableName}} AS pb
              WHERE EXISTS
                  (SELECT 1
                   FROM {{_simpleCcResultRepo.DelimetedTableName}} AS ccr
                   INNER JOIN {{_doiRepo.DelimetedTableName}} AS doi
                     ON doi.{{doiIdColumn}} = pb.{{pbDoiIdColumn}}
                   WHERE ccr.{{ccResultPbIdColumn}} = pb.{{pbIdColumn}}
                     AND doi.{{doiTenantIdColumn}} = {0})
                OR EXISTS
                  (SELECT 1
                   FROM {{_simpleCcResultRepo.DelimetedTableName}} AS ccr
                   INNER JOIN {{_permissionRepo.DelimetedTableName}} AS doip
                     ON ccr.{{ccResultCcIdColumn}} = ANY(doip.{{permissionCcIdsColumn}})
                   INNER JOIN {{_doiRepo.DelimetedTableName}} AS doi
                     ON doi.{{doiBasisIdColumn}} = doip.{{permissionBasisDoiIdColumn}}
                   WHERE {{ccResultPbIdColumn}} = pb.{{pbIdColumn}}
                     AND doip.{{permissionTenantIdColumn}} = {0}
                     AND doi.{{doiPartialCcResultsColumn}} = TRUE)
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
