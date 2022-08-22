// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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

    public Task UnsetAllNextExecutionDates(Guid contestId)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.ContestId);
        var nextExecutionColName = GetDelimitedColumnName(x => x.NextExecution);
        return Context.Database.ExecuteSqlRawAsync(
            $"UPDATE {DelimitedSchemaAndTableName} SET {nextExecutionColName} = NULL WHERE {contestIdColName} = {{0}}",
            contestId);
    }

    public async Task<ResultExportConfiguration?> FetchAndLock(DateTime now)
    {
        var nextExecutionColName = GetDelimitedColumnName(x => x.NextExecution);
        return await Context.ResultExportConfigurations
            .FromSqlRaw($"SELECT * FROM {DelimitedSchemaAndTableName} WHERE {nextExecutionColName} < {{0}} LIMIT 1 FOR UPDATE SKIP LOCKED", now)
            .FirstOrDefaultAsync();
    }

    public Task<List<ResultExportConfiguration>> GetActiveOrTestingPhaseExportConfigurations(Guid exportConfigurationId)
    {
        return Set
            .Where(x => x.ExportConfigurationId == exportConfigurationId
                        && (x.Contest!.State == ContestState.Active || x.Contest!.State == ContestState.TestingPhase))
            .ToListAsync();
    }
}
