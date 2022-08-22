// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class SimplePoliticalBusinessBuilder<TPoliticalBusiness>
    where TPoliticalBusiness : PoliticalBusiness
{
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _domainOfInfluenceRepo;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _politicalBusinessRepo;
    private readonly SimplePoliticalBusinessTranslationRepo _politicalBusinessTranslationRepo;
    private readonly SimpleCountingCircleResultRepo _countingCircleResultRepo;
    private readonly IDbRepository<DataContext, VotingCardResultDetail> _votingCardResultDetailRepo;
    private readonly IMapper _mapper;

    public SimplePoliticalBusinessBuilder(
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> politicalBusinessRepo,
        SimplePoliticalBusinessTranslationRepo politicalBusinessTranslationRepo,
        SimpleCountingCircleResultRepo countingCircleResultRepo,
        IDbRepository<DataContext, VotingCardResultDetail> votingCardResultDetailRepo,
        IMapper mapper)
    {
        _countingCircleRepo = countingCircleRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _politicalBusinessRepo = politicalBusinessRepo;
        _politicalBusinessTranslationRepo = politicalBusinessTranslationRepo;
        _countingCircleResultRepo = countingCircleResultRepo;
        _votingCardResultDetailRepo = votingCardResultDetailRepo;
        _mapper = mapper;
    }

    public async Task Create(TPoliticalBusiness politicalBusiness)
    {
        var simplePoliticalBusiness = _mapper.Map<SimplePoliticalBusiness>(politicalBusiness);

        await _politicalBusinessRepo.Create(simplePoliticalBusiness);
        await _countingCircleResultRepo.Sync(simplePoliticalBusiness.Id, simplePoliticalBusiness.DomainOfInfluenceId, false);
    }

    public async Task Update(TPoliticalBusiness politicalBusiness, bool testingPhaseEnded, bool syncCountingCircles = true)
    {
        var simplePoliticalBusiness = await _politicalBusinessRepo.GetByKey(politicalBusiness.Id)
            ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), politicalBusiness.Id);
        _mapper.Map(politicalBusiness, simplePoliticalBusiness);

        // Only remove translations if new ones are provided
        if (simplePoliticalBusiness.Translations.Count > 0)
        {
            await _politicalBusinessTranslationRepo.DeleteRelatedTranslations(simplePoliticalBusiness.Id);
        }

        await _politicalBusinessRepo.Update(simplePoliticalBusiness);

        if (syncCountingCircles)
        {
            await _countingCircleResultRepo.Sync(simplePoliticalBusiness.Id, simplePoliticalBusiness.DomainOfInfluenceId, testingPhaseEnded);
        }
    }

    public async Task MoveToNewContest(Guid politicalBusinessId, Guid newContestId)
    {
        var politicalBusiness = await _politicalBusinessRepo.Query()
            .AsSplitQuery()
            .Where(x => x.Id == politicalBusinessId)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.SimpleResults)
            .ThenInclude(x => x.CountingCircle)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(politicalBusinessId);

        var countingCircleMap = await _countingCircleRepo.Query()
            .Where(cc => cc.SnapshotContestId == newContestId)
            .ToDictionaryAsync(x => x.BasisCountingCircleId, x => x.Id);

        var domainOfInfluenceIdMap = await _domainOfInfluenceRepo.Query()
            .Where(x => x.SnapshotContestId == newContestId)
            .ToDictionaryAsync(x => x.BasisDomainOfInfluenceId, x => x.Id);

        politicalBusiness.DomainOfInfluenceId = MapToNewId(domainOfInfluenceIdMap, politicalBusiness.DomainOfInfluence.BasisDomainOfInfluenceId);
        politicalBusiness.DomainOfInfluence = null!;

        foreach (var countingCircleResult in politicalBusiness.SimpleResults)
        {
            countingCircleResult.CountingCircleId = MapToNewId(countingCircleMap, countingCircleResult.CountingCircle!.BasisCountingCircleId);
            countingCircleResult.CountingCircle = null!;
        }

        politicalBusiness.ContestId = newContestId;
        await _politicalBusinessRepo.Update(politicalBusiness);
    }

    public async Task AdjustCountOfSecondaryBusinesses(Guid primaryBusinessId, int delta)
    {
        var business = await _politicalBusinessRepo.GetByKey(primaryBusinessId)
            ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), primaryBusinessId);
        business.CountOfSecondaryBusinesses += delta;
        await _politicalBusinessRepo.Update(business);
    }

    public async Task Delete(Guid politicalBusinessId)
    {
        await DeleteRelatedVotingCards(politicalBusinessId);
        await _politicalBusinessRepo.DeleteByKey(politicalBusinessId);
    }

    private async Task DeleteRelatedVotingCards(Guid politicalBusinessId)
    {
        var politicalBusiness = await _politicalBusinessRepo.Query()
                                    .AsSplitQuery()
                                    .Include(x => x.DomainOfInfluence)
                                    .Include(x => x.Contest).ThenInclude(x => x.CountingCircleDetails).ThenInclude(x => x.VotingCards)
                                    .FirstOrDefaultAsync(x => x.Id == politicalBusinessId)
                                ?? throw new EntityNotFoundException(politicalBusinessId);

        var hasPoliticalBusinessWithDomainOfInfluenceType = await _politicalBusinessRepo.Query()
            .Include(x => x.DomainOfInfluence)
            .Where(x => x.ContestId == politicalBusiness.ContestId && x.Id != politicalBusinessId)
            .AnyAsync(x => x.DomainOfInfluence.Type == politicalBusiness.DomainOfInfluence.Type);

        if (hasPoliticalBusinessWithDomainOfInfluenceType)
        {
            return;
        }

        var votingCardIds = politicalBusiness.Contest.CountingCircleDetails
            .SelectMany(ccDetails => ccDetails.VotingCards)
            .Where(vc => vc.DomainOfInfluenceType == politicalBusiness.DomainOfInfluence.Type)
            .Select(vc => vc.Id);
        await _votingCardResultDetailRepo.DeleteRangeByKey(votingCardIds);
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
