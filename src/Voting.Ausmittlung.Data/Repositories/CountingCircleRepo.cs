// (c) Copyright 2022 by Abraxas Informatik AG
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

    public Task SetMustUpdateContactPerson(Guid contestId)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.SnapshotContestId);
        var mustUpdateContactPersonColName = GetDelimitedColumnName(x => x.MustUpdateContactPersons);
        return Context.Database.ExecuteSqlRawAsync(
            $"UPDATE {DelimitedSchemaAndTableName} SET {mustUpdateContactPersonColName} = TRUE WHERE {contestIdColName} = {{0}}",
            contestId);
    }
}
