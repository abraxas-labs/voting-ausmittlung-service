// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.TemporaryData.Repositories;

public class ExportLogEntryRepo : DbRepository<TemporaryDataContext, ExportLogEntry>
{
    public ExportLogEntryRepo(TemporaryDataContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Gets whether the tenant has exported a certain template more than specified within a timespan.
    /// Also locks the read columns.
    /// </summary>
    /// <param name="exportKey">Export template key.</param>
    /// <param name="tenantId">Tenant id.</param>
    /// <param name="from">Beginning of the timespan.</param>
    /// <param name="to">End of the timespan.</param>
    /// <param name="maxCount">Maximum count of allowed entries.</param>
    /// <returns>Whether the tenant has exported a certain template more than specified within a timespan.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task<bool> HasReachedLimitAndLock(string exportKey, string tenantId, DateTime from, DateTime to, int maxCount)
    {
        var tenantIdColName = GetDelimitedColumnName(x => x.TenantId);
        var exportKeyColName = GetDelimitedColumnName(x => x.ExportKey);
        var timestampColName = GetDelimitedColumnName(x => x.Timestamp);

        // locks dont work with aggregate functions, thats why we read the whole record for counting.
        var entries = await Context.Database.SqlQueryRaw<ExportLogEntry>(
                $@"SELECT *
                FROM {DelimitedSchemaAndTableName}
                WHERE {tenantIdColName} = {{0}}
                AND {exportKeyColName} = {{1}}
                AND {timestampColName} > {{2}}
                AND {timestampColName} <= {{3}}
                FOR UPDATE NOWAIT",
                tenantId,
                exportKey,
                from,
                to)
            .ToListAsync();

        return entries.Count >= maxCount;
    }
}
