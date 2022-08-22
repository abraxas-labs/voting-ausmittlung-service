// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class PoliticalBusinessToNewContestMover<TPoliticalBusiness, TPoliticalBusinessRepo>
    where TPoliticalBusiness : PoliticalBusiness, new()
    where TPoliticalBusinessRepo : PoliticalBusinessRepo<TPoliticalBusiness>
{
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _domainOfInfluenceRepo;
    private readonly TPoliticalBusinessRepo _pbRepo;

    public PoliticalBusinessToNewContestMover(
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo,
        TPoliticalBusinessRepo pbRepo)
    {
        _countingCircleRepo = countingCircleRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _pbRepo = pbRepo;
    }

    public async Task Move(Guid politicalBusinessId, Guid newContestId)
    {
        var politicalBusiness = await _pbRepo.QueryWithResults()
            .Include(pb => pb.DomainOfInfluence)
            .FirstOrDefaultAsync(pb => pb.Id == politicalBusinessId)
            ?? throw new EntityNotFoundException(politicalBusinessId);

        var countingCircleMap = await _countingCircleRepo.Query()
            .Where(cc => cc.SnapshotContestId == newContestId)
            .ToDictionaryAsync(x => x.BasisCountingCircleId, x => x.Id);

        var domainOfInfluenceMap = await _domainOfInfluenceRepo.Query()
            .Where(cc => cc.SnapshotContestId == newContestId)
            .ToDictionaryAsync(x => x.BasisDomainOfInfluenceId, x => x.Id);

        politicalBusiness.DomainOfInfluenceId = MapToNewId(domainOfInfluenceMap, politicalBusiness.DomainOfInfluence.BasisDomainOfInfluenceId);
        politicalBusiness.DomainOfInfluence = null!;

        foreach (var countingCircleResult in politicalBusiness.CountingCircleResults)
        {
            countingCircleResult.CountingCircleId = MapToNewId(countingCircleMap, countingCircleResult.CountingCircle.BasisCountingCircleId);
            countingCircleResult.CountingCircle = null!;
        }

        politicalBusiness.ContestId = newContestId;
        await _pbRepo.Update(politicalBusiness);
    }

    private Guid MapToNewId(IReadOnlyDictionary<Guid, Guid> basisIdToNewIdMapping, Guid basisId)
    {
        if (!basisIdToNewIdMapping.TryGetValue(basisId, out var newId))
        {
            throw new InvalidOperationException($"Cannot map {basisId} to a new id");
        }

        return newId;
    }
}
