// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class DomainOfInfluencePermissionRepo : DbRepository<DataContext, DomainOfInfluencePermissionEntry>
{
    public DomainOfInfluencePermissionRepo(DataContext context)
        : base(context)
    {
    }

    internal string DelimetedTableName => DelimitedSchemaAndTableName;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task Replace(IEnumerable<DomainOfInfluencePermissionEntry> entries)
    {
        var isFinalColName = GetDelimitedColumnName(x => x.IsFinal);
        await Context.Database.ExecuteSqlRawAsync($"DELETE FROM {DelimitedSchemaAndTableName} WHERE {isFinalColName} = FALSE");
        Set.AddRange(entries);
        await Context.SaveChangesAsync();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task SetContestPermissionsFinal(Guid contestId)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.ContestId);
        var isFinalColName = GetDelimitedColumnName(x => x.IsFinal);
        await Context.Database.ExecuteSqlRawAsync(
            $"UPDATE {DelimitedSchemaAndTableName} SET {isFinalColName} = TRUE WHERE {contestIdColName} = {{0}}", contestId);
    }

    internal string GetColumnName<TProp>(Expression<Func<DomainOfInfluencePermissionEntry, TProp>> memberAccess)
        => GetDelimitedColumnName(memberAccess);
}
