// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Database.Utils;

namespace Voting.Ausmittlung.Data.Repositories;

public class DomainOfInfluencePermissionRepo : DbRepository<DataContext, DomainOfInfluencePermissionEntry>
{
    public DomainOfInfluencePermissionRepo(DataContext context)
        : base(context)
    {
    }

    internal string DelimetedTableName => DelimitedSchemaAndTableName;

    public async Task Replace(IEnumerable<DomainOfInfluencePermissionEntry> entries)
    {
        using var disposable = PerformanceUtil.DisableAutoChangeDetection(Context);
        await Query()
            .Where(x => !x.IsFinal)
            .ExecuteDeleteAsync();
        await CreateRange(entries);
    }

    internal string GetColumnName<TProp>(Expression<Func<DomainOfInfluencePermissionEntry, TProp>> memberAccess)
        => GetDelimitedColumnName(memberAccess);
}
