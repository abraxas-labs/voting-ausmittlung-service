// (c) Copyright 2022 by Abraxas Informatik AG
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

    public SimplePoliticalBusinessRepo(
        DataContext context,
        DomainOfInfluencePermissionRepo permissionRepo,
        SimpleCountingCircleResultRepo simpleCcResultRepo)
        : base(context)
    {
        _permissionRepo = permissionRepo;
        _simpleCcResultRepo = simpleCcResultRepo;
    }

    public IQueryable<SimplePoliticalBusiness> BuildAccessibleQuery(string tenantId)
    {
        var permissionCcIdsColumn = _permissionRepo.GetColumnName(x => x.CountingCircleIds);
        var permissionTenantIdColumn = _permissionRepo.GetColumnName(x => x.TenantId);
        var ccResultPbIdColumn = _simpleCcResultRepo.GetColumnName(x => x.PoliticalBusinessId);
        var ccResultCcIdColumn = _simpleCcResultRepo.GetColumnName(x => x.CountingCircleId);
        var idColumn = GetDelimitedColumnName(x => x.Id);

        return Set.FromSqlRaw(
            $@"WITH permission_query AS (
                SELECT unnest({permissionCcIdsColumn}) as ccids FROM {_permissionRepo.DelimetedTableName} WHERE {permissionTenantIdColumn} = {{0}}
            )
            SELECT * FROM {DelimitedSchemaAndTableName} AS pb WHERE EXISTS (
                SELECT 1 FROM {_simpleCcResultRepo.DelimetedTableName} AS ccr WHERE ccr.{ccResultPbIdColumn} = pb.{idColumn}
                AND ccr.{ccResultCcIdColumn} IN (SELECT ccids FROM permission_query)
            )",
            tenantId);
    }
}
