// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ResultImportRepo : DbRepository<DataContext, ResultImport>
{
    public ResultImportRepo(DataContext context)
        : base(context)
    {
    }

    public Task DeleteOfContest(Guid contestId)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.ContestId);
        return Context.Database.ExecuteSqlRawAsync($"DELETE FROM {DelimitedSchemaAndTableName} WHERE {contestIdColName} = {{0}}", contestId);
    }
}
