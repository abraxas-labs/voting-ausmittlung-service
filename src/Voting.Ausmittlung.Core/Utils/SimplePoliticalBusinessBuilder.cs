// (c) Copyright by Abraxas Informatik AG
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
    private readonly AggregatedContestCountingCircleDetailsBuilder _aggregatedContestCountingCircleDetailsBuilder;

    public SimplePoliticalBusinessBuilder(
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> politicalBusinessRepo,
        SimplePoliticalBusinessTranslationRepo politicalBusinessTranslationRepo,
        SimpleCountingCircleResultRepo countingCircleResultRepo,
        IDbRepository<DataContext, VotingCardResultDetail> votingCardResultDetailRepo,
        IMapper mapper,
        AggregatedContestCountingCircleDetailsBuilder aggregatedContestCountingCircleDetailsBuilder)
    {
        _countingCircleRepo = countingCircleRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _politicalBusinessRepo = politicalBusinessRepo;
        _politicalBusinessTranslationRepo = politicalBusinessTranslationRepo;
        _countingCircleResultRepo = countingCircleResultRepo;
        _votingCardResultDetailRepo = votingCardResultDetailRepo;
        _mapper = mapper;
        _aggregatedContestCountingCircleDetailsBuilder = aggregatedContestCountingCircleDetailsBuilder;
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

    public async Task UpdateSubTypeIfNecessary(TPoliticalBusiness politicalBusiness)
    {
        var simplePoliticalBusiness = _mapper.Map<SimplePoliticalBusiness>(politicalBusiness);
        await _politicalBusinessRepo.Query()
            .Where(x => x.Id == simplePoliticalBusiness.Id && x.PoliticalBusinessSubType != simplePoliticalBusiness.PoliticalBusinessSubType)
            .ExecuteUpdateAsync(x => x.SetProperty(prop => prop.PoliticalBusinessSubType, simplePoliticalBusiness.PoliticalBusinessSubType));
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
        await MoveToNewContest(politicalBusiness, newContestId);
    }

    public async Task MoveToNewContest(SimplePoliticalBusiness politicalBusinessWithResults, Guid newContestId)
    {
        var countingCircleMap = await _countingCircleRepo.Query()
            .Where(cc => cc.SnapshotContestId == newContestId)
            .ToDictionaryAsync(x => x.BasisCountingCircleId, x => x.Id);

        var domainOfInfluenceIdMap = await _domainOfInfluenceRepo.Query()
            .Where(x => x.SnapshotContestId == newContestId)
            .ToDictionaryAsync(x => x.BasisDomainOfInfluenceId, x => x.Id);

        politicalBusinessWithResults.DomainOfInfluenceId = MapToNewId(domainOfInfluenceIdMap, politicalBusinessWithResults.DomainOfInfluence.BasisDomainOfInfluenceId);
        politicalBusinessWithResults.DomainOfInfluence = null!;

        foreach (var countingCircleResult in politicalBusinessWithResults.SimpleResults)
        {
            countingCircleResult.CountingCircleId = MapToNewId(countingCircleMap, countingCircleResult.CountingCircle!.BasisCountingCircleId);
            countingCircleResult.CountingCircle = null!;
        }

        politicalBusinessWithResults.ContestId = newContestId;
        await _politicalBusinessRepo.Update(politicalBusinessWithResults);
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

        await DeleteRelatedVotingCardsAndResetAggregatedDetails(
            politicalBusiness.ContestId,
            politicalBusiness.Contest.CountingCircleDetails.ToList(),
            politicalBusiness.DomainOfInfluence.Type);
    }

    private Guid MapToNewId(IReadOnlyDictionary<Guid, Guid> basisIdToNewIdMapping, Guid basisId)
    {
        if (!basisIdToNewIdMapping.TryGetValue(basisId, out var newId))
        {
            throw new InvalidOperationException($"Cannot map {basisId} to a new id");
        }

        return newId;
    }

    private async Task DeleteRelatedVotingCardsAndResetAggregatedDetails(Guid contestId, IReadOnlyCollection<ContestCountingCircleDetails> ccDetails, DomainOfInfluenceType doiType)
    {
        // create a new empty cc details object to only subtract related voting card results from the aggregated details.
        var preparedCcDetails = ccDetails
            .Where(ccDetail => ccDetail.VotingCards.Any(vc => vc.DomainOfInfluenceType == doiType))
            .Select(x =>
                new ContestCountingCircleDetails
                {
                    Id = x.Id,
                    CountingCircleId = x.CountingCircleId,
                    VotingCards = x.VotingCards.Where(vc => vc.DomainOfInfluenceType == doiType).ToList(),
                })
            .ToList();

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(contestId, preparedCcDetails, true);

        var votingCardIds = preparedCcDetails
            .SelectMany(ccDetails => ccDetails.VotingCards)
            .Select(vc => vc.Id);

        await _votingCardResultDetailRepo.DeleteRangeByKey(votingCardIds);
    }
}
