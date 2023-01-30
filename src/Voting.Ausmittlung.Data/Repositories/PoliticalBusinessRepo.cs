// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public abstract class PoliticalBusinessRepo<T> : DbRepository<DataContext, T>
    where T : PoliticalBusiness, new()
{
    protected PoliticalBusinessRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<int> CountOfCountingCircles(Guid politicalBusinessId)
    {
        return await Set.Where(x => x.Id == politicalBusinessId)
            .SelectMany(x => x.DomainOfInfluence.CountingCircles)
            .CountAsync();
    }

    public abstract IQueryable<T> QueryWithResults();
}
