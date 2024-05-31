// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class CountingCircleRepo : DbRepository<DataContext, CountingCircle>
{
    public CountingCircleRepo(DataContext context)
        : base(context)
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public Task SetMustUpdateContactPerson(Guid contestId)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.SnapshotContestId);
        var mustUpdateContactPersonColName = GetDelimitedColumnName(x => x.MustUpdateContactPersons);
        return Context.Database.ExecuteSqlRawAsync(
            $"UPDATE {DelimitedSchemaAndTableName} SET {mustUpdateContactPersonColName} = TRUE WHERE {contestIdColName} = {{0}}",
            contestId);
    }
}
