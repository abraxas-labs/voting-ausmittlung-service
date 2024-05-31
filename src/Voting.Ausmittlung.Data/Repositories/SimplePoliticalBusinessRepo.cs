// (c) Copyright 2024 by Abraxas Informatik AG
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened interpolated string parameters.")]
    public IQueryable<SimplePoliticalBusiness> BuildAccessibleQuery(string tenantId, bool readOnlyOwned)
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

        // If readOnlyOwned is true, then only "owned" political businesses should be returned.
        // This means that the domain of influence of the political business must have a matching tenant id.
        // An exception is made for domain of influences with the special "view counting circle partial results" flag.
        var onlyOwnedRestriction = readOnlyOwned
            ? $@"AND EXISTS (
                SELECT 1 FROM {_doiRepo.DelimetedTableName} AS doi
                WHERE (doi.{doiIdColumn} = pb.{pbDoiIdColumn} AND doi.{doiTenantIdColumn} = {{0}})
                OR (doi.{doiPartialCcResultsColumn} = TRUE AND ccr.{ccResultCcIdColumn} IN (SELECT ccids FROM permission_query AS pq WHERE pq.doiid = doi.{doiBasisIdColumn}))
            )"
            : string.Empty;

        // Access to a political business is decided by the participating counting circles.
        // The tenant must either be the responsible authority of a participating counting circle
        // or the responsible authority of a domain of influence which has an assigned participating counting circle.
        return Set.FromSqlRaw(
            $@"WITH permission_query AS (
                SELECT unnest({permissionCcIdsColumn}) as ccids, {permissionBasisDoiIdColumn} as doiid FROM {_permissionRepo.DelimetedTableName}
                WHERE {permissionTenantIdColumn} = {{0}}
            )
            SELECT pb.* FROM {DelimitedSchemaAndTableName} AS pb WHERE EXISTS (
                SELECT 1 FROM {_simpleCcResultRepo.DelimetedTableName} AS ccr WHERE ccr.{ccResultPbIdColumn} = pb.{pbIdColumn}
                AND ccr.{ccResultCcIdColumn} IN (SELECT ccids FROM permission_query)
                {onlyOwnedRestriction}
            )",
            tenantId);
    }
}
