// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ResultExportConfigurationRepo : DbRepository<DataContext, ResultExportConfiguration>
{
    public ResultExportConfigurationRepo(DataContext context)
        : base(context)
    {
    }

    [SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public Task UnsetAllNextExecutionDates(Guid contestId)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.ContestId);
        var nextExecutionColName = GetDelimitedColumnName(x => x.NextExecution);
        return Context.Database.ExecuteSqlRawAsync(
            $"UPDATE {DelimitedSchemaAndTableName} SET {nextExecutionColName} = NULL WHERE {contestIdColName} = {{0}}",
            contestId);
    }

    [SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task<ResultExportConfiguration?> FetchAndLock(DateTime now)
    {
        var nextExecutionColName = GetDelimitedColumnName(x => x.NextExecution);
        return await Context.ResultExportConfigurations
            .FromSqlRaw($"SELECT * FROM {DelimitedSchemaAndTableName} WHERE {nextExecutionColName} < {{0}} LIMIT 1 FOR UPDATE SKIP LOCKED", now)
            .FirstOrDefaultAsync();
    }
}
