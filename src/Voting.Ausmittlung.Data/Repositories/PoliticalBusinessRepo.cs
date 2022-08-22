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

public abstract class PoliticalBusinessRepo<T> : DbRepository<DataContext, T>
    where T : PoliticalBusiness, new()
{
    private readonly DomainOfInfluenceRepo _domainOfInfluenceRepo;
    private readonly CountingCircleRepo _countingCircleRepo;

    protected PoliticalBusinessRepo(DataContext context, DomainOfInfluenceRepo domainOfInfluenceRepo, CountingCircleRepo countingCircleRepo)
        : base(context)
    {
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _countingCircleRepo = countingCircleRepo;
    }

    public async Task<int> CountOfCountingCircles(Guid politicalBusinessId)
    {
        return await Set.Where(x => x.Id == politicalBusinessId)
            .SelectMany(x => x.DomainOfInfluence.CountingCircles)
            .CountAsync();
    }

    public abstract IQueryable<T> QueryWithResults();

    private Guid MapToNewId(IReadOnlyDictionary<Guid, Guid> basisIdToNewIdMapping, Guid basisId)
    {
        if (!basisIdToNewIdMapping.TryGetValue(basisId, out var newId))
        {
            throw new InvalidOperationException($"Cannot map {basisId} to a new id");
        }

        return newId;
    }
}
