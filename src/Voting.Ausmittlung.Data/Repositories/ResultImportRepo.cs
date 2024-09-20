// (c) Copyright by Abraxas Informatik AG
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public Task DeleteOfContest(Guid contestId)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.ContestId);
        return Context.Database.ExecuteSqlRawAsync($"DELETE FROM {DelimitedSchemaAndTableName} WHERE {contestIdColName} = {{0}}", contestId);
    }
}
