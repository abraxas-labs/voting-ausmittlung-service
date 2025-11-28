// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ContestCountingCircleDetailsBuilder
{
    private readonly DomainOfInfluenceCountingCircleRepo _doiCountingCirclesRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _domainOfInfluenceRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly AggregatedContestCountingCircleDetailsBuilder _aggregatedContestCountingCircleDetailsBuilder;
    private readonly IDbRepository<DataContext, CountOfVotersInformationSubTotal> _countOfVotersInformationSubTotalRepo;
    private readonly DataContext _dataContext;
    private readonly CountingCircleResultBuilder _ccResultBuilder;

    public ContestCountingCircleDetailsBuilder(
        DomainOfInfluenceCountingCircleRepo doiCountingCirclesRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        AggregatedContestCountingCircleDetailsBuilder aggregatedContestCountingCircleDetailsBuilder,
        DataContext dataContext,
        IDbRepository<DataContext, CountOfVotersInformationSubTotal> countOfVotersInformationSubTotalRepo,
        CountingCircleResultBuilder ccResultBuilder)
    {
        _doiCountingCirclesRepo = doiCountingCirclesRepo;
        _contestRepo = contestRepo;
        _ccDetailsRepo = ccDetailsRepo;
        _countingCircleRepo = countingCircleRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _simpleResultRepo = simpleResultRepo;
        _aggregatedContestCountingCircleDetailsBuilder = aggregatedContestCountingCircleDetailsBuilder;
        _dataContext = dataContext;
        _countOfVotersInformationSubTotalRepo = countOfVotersInformationSubTotalRepo;
        _ccResultBuilder = ccResultBuilder;
    }

    internal async Task SyncForDomainOfInfluences(IEnumerable<Guid> doiIds)
    {
        var contests = await _contestRepo.Query()
            .AsSplitQuery()
            .WhereInTestingPhase()
            .Where(x => doiIds.Contains(x.DomainOfInfluenceId))
            .Include(x => x.DomainOfInfluence.CountingCircles)
            .ThenInclude(x => x.CountingCircle)
            .ToListAsync();

        foreach (var contest in contests)
        {
            await Sync(contest, contest.DomainOfInfluence.CountingCircles);
        }
    }

    internal async Task<IReadOnlyCollection<ContestCountingCircleDetails>> SyncAndResetEVoting(Contest contest)
    {
        var countingCircles = await _doiCountingCirclesRepo.Query()
            .Where(c => c.DomainOfInfluenceId == contest.DomainOfInfluenceId)
            .Include(x => x.CountingCircle)
            .ToListAsync();

        return await Sync(contest, countingCircles);
    }

    internal async Task ResetVotingCards(Guid contestId, Guid? countingCircleId, VotingChannel channel)
    {
        var query = _ccDetailsRepo
            .Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == contestId && x.VotingCards.Any(vc => vc.Channel == channel));

        if (countingCircleId.HasValue)
        {
            query = query.Where(x => x.CountingCircleId == countingCircleId.Value);
        }

        var ccDetails = await
            query
            .Include(x => x.VotingCards.Where(vc => vc.Channel == channel))
            .ToListAsync();

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedVotingCards(contestId, ccDetails, true);

        foreach (var votingCard in ccDetails.SelectMany(x => x.VotingCards))
        {
            votingCard.CountOfReceivedVotingCards = 0;
        }

        await _ccDetailsRepo.UpdateRange(ccDetails);
    }

    internal async Task ResetConventionalVotingCardsAndSubTotals(Guid contestId, Guid basisCountingCircleId)
    {
        var ccDetails = await _ccDetailsRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards.Where(vc => vc.Channel != VotingChannel.EVoting))
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.CountingCircle)
            .Where(x => x.ContestId == contestId && x.CountingCircle.BasisCountingCircleId == basisCountingCircleId && x.VotingCards.Any(vc => vc.Channel != VotingChannel.EVoting))
            .SingleOrDefaultAsync();

        if (ccDetails == null)
        {
            return;
        }

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(ccDetails, true);

        ccDetails.ResetVotingCardsAndSubTotals();

        // don't want to save counting circle again
        ccDetails.CountingCircle = null!;

        await _ccDetailsRepo.Update(ccDetails);
        await _ccResultBuilder.UpdateCountOfVotersForCountingCircleResults(ccDetails, true);
    }

    internal async Task SyncForDomainOfInfluence(Guid politicalBusinessId, Guid contestId, Guid domainOfInfluenceId)
    {
        var detailsByCountingCircleId = await LoadCountingCircleDetailsByCountingCircleId(contestId);

        var contestSimpleResults = await _simpleResultRepo.Query()
            .Include(x => x.PoliticalBusiness!.Contest.DomainOfInfluence)
            .Include(x => x.PoliticalBusiness!.DomainOfInfluence)
            .Include(x => x.CountingCircle)
            .Where(x => x.PoliticalBusiness!.ContestId == contestId)
            .ToListAsync();

        var simpleResults = contestSimpleResults.Where(x => x.PoliticalBusinessId == politicalBusinessId).ToList();
        var doisByCcId = contestSimpleResults
            .GroupBy(x => x.CountingCircleId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.PoliticalBusiness!.DomainOfInfluence).ToList());

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(contestId, detailsByCountingCircleId.Values, true);

        await RemoveNotNeededVoterTypesInSubTotals(simpleResults, detailsByCountingCircleId, doisByCcId);

        // We need to reload a fresh state of the cc details, because after removing the unneeded sub totals they are out of sync.
        detailsByCountingCircleId = await LoadCountingCircleDetailsByCountingCircleId(contestId);
        await CreateMissingVotingCardsAndSubTotalsInElectorate(simpleResults, contestId, domainOfInfluenceId, detailsByCountingCircleId);

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(contestId, detailsByCountingCircleId.Values, false);

        // Changes can only be made during the testing phase, some corresponding CcResults could are already audited
        // The CcResult state check has to be skipped.
        await _ccResultBuilder.UpdateCountOfVotersForCountingCircleResults(detailsByCountingCircleId.Values, true);
    }

    internal async Task CreateMissingVotingCardsAndAggregatedDetails(Guid politicalBusinessId, Guid contestId, Guid domainOfInfluenceId)
    {
        var simpleResults = await _simpleResultRepo.Query()
            .Include(x => x.PoliticalBusiness!.Contest.DomainOfInfluence)
            .Include(x => x.PoliticalBusiness!.DomainOfInfluence)
            .Include(x => x.CountingCircle)
            .Where(x => x.PoliticalBusinessId == politicalBusinessId)
            .ToListAsync();

        var detailsByCountingCircleId = await LoadCountingCircleDetailsByCountingCircleId(contestId);
        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(contestId, detailsByCountingCircleId.Values, true);
        await CreateMissingVotingCardsAndSubTotalsInElectorate(simpleResults, contestId, domainOfInfluenceId, detailsByCountingCircleId);
        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(contestId, detailsByCountingCircleId.Values, false);
    }

    private async Task RemoveNotNeededVoterTypesInSubTotals(
        List<SimpleCountingCircleResult> simpleResults,
        Dictionary<Guid, ContestCountingCircleDetails> detailsByCountingCircleId,
        Dictionary<Guid, List<DomainOfInfluence>> doisByCcId)
    {
        var subTotalIdsToDelete = new List<Guid>();
        var detailsToUpdate = new HashSet<ContestCountingCircleDetails>();

        foreach (var result in simpleResults)
        {
            if (!detailsByCountingCircleId.TryGetValue(result.CountingCircleId, out var details)
                || !doisByCcId.TryGetValue(result.CountingCircleId, out var domainOfInfluences))
            {
                continue;
            }

            var foreignerSubTotals = details.CountOfVotersInformationSubTotals.Where(d => d.VoterType == VoterType.Foreigner).ToList();
            var minorSubTotals = details.CountOfVotersInformationSubTotals.Where(d => d.VoterType == VoterType.Minor).ToList();

            // We only need to remove the sub total if a voter type is not needed anymore. The writer (ccDetailsCreate/Update) is responsible to create the sub total.
            if (!domainOfInfluences.Any(doi => doi.HasForeignerVoters) && foreignerSubTotals.Count != 0)
            {
                var foreignerSubTotalIds = foreignerSubTotals.ConvertAll(s => s.Id);
                subTotalIdsToDelete.AddRange(foreignerSubTotalIds);
                details.CountOfVotersInformationSubTotals = details.CountOfVotersInformationSubTotals.Where(s => !foreignerSubTotalIds.Contains(s.Id)).ToList();
                detailsToUpdate.Add(details);
            }

            if (!domainOfInfluences.Any(doi => doi.HasMinorVoters) && minorSubTotals.Count != 0)
            {
                var minorSubTotalIds = minorSubTotals.ConvertAll(s => s.Id);
                subTotalIdsToDelete.AddRange(minorSubTotalIds);
                details.CountOfVotersInformationSubTotals = details.CountOfVotersInformationSubTotals.Where(s => !minorSubTotalIds.Contains(s.Id)).ToList();
                detailsToUpdate.Add(details);
            }
        }

        if (subTotalIdsToDelete.Count > 0)
        {
            await _countOfVotersInformationSubTotalRepo.DeleteRangeByKey(subTotalIdsToDelete);
            await _ccDetailsRepo.UpdateRange(detailsToUpdate);
        }
    }

    private async Task CreateMissingVotingCardsAndSubTotalsInElectorate(List<SimpleCountingCircleResult> simpleResults, Guid contestId, Guid domainOfInfluenceId, Dictionary<Guid, ContestCountingCircleDetails> detailsByCountingCircleId)
    {
        var domainOfInfluence = await _domainOfInfluenceRepo.GetByKey(domainOfInfluenceId)
            ?? throw new EntityNotFoundException(nameof(DomainOfInfluence), domainOfInfluenceId);

        var countingCirclesByBasisCountingCircleId = await _countingCircleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Electorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
            .Include(x => x.ContestElectorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
            .Where(x => x.SnapshotContestId == contestId)
            .ToDictionaryAsync(x => x.BasisCountingCircleId);

        var resultsByCountingCircleId = await _simpleResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.PoliticalBusiness!.Translations)
            .Include(x => x.PoliticalBusiness!.DomainOfInfluence)
            .Where(x => x.PoliticalBusiness!.ContestId == contestId
                        && x.PoliticalBusiness.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection)
            .OrderBy(x => domainOfInfluence.Type.IsPolitical() ? x.PoliticalBusiness!.DomainOfInfluence.Type : 0)
            .ThenBy(x => x.PoliticalBusiness!.PoliticalBusinessNumber)
            .GroupBy(x => x.CountingCircleId)
            .ToDictionaryAsync(x => x.Key, x => x.ToList());

        var detailsToUpdate = new List<ContestCountingCircleDetails>();
        foreach (var result in simpleResults)
        {
            // details contain already the desired voting cards for this domain of influence type or voting cards are empty
            if (!detailsByCountingCircleId.TryGetValue(result.CountingCircleId, out var details) ||
                !countingCirclesByBasisCountingCircleId.TryGetValue(result.CountingCircle!.BasisCountingCircleId, out var countingCircle) ||
                !resultsByCountingCircleId.TryGetValue(result.CountingCircleId, out var results))
            {
                continue;
            }

            var hasMissingVotingCards = details.VotingCards.Count > 0 && details.VotingCards.All(x => x.DomainOfInfluenceType != result.PoliticalBusiness!.DomainOfInfluence.Type);
            var hasMissingSubTotals = details.CountOfVotersInformationSubTotals.Count > 0 && details.CountOfVotersInformationSubTotals.All(x => x.DomainOfInfluenceType != result.PoliticalBusiness!.DomainOfInfluence.Type);

            if (!hasMissingVotingCards && !hasMissingSubTotals)
            {
                continue;
            }

            details.OrderVotingCardsAndSubTotals();

            var electorateSummary = ContestCountingCircleElectorateSummaryBuilder.Build(
                countingCircle,
                details,
                results.Select(r => r.PoliticalBusiness!.DomainOfInfluence.Type).ToHashSet());

            var domainOfInfluenceType = result.PoliticalBusiness!.DomainOfInfluence.Type;
            var electorate = electorateSummary.EffectiveElectorates.SingleOrDefault(x => x.DomainOfInfluenceTypes.Contains(domainOfInfluenceType));
            if (electorate == null)
            {
                continue;
            }

            // it does not matter which type is chosen, as they all have the same values in an electorate
            var domainOfInfluenceTypeToCopy = electorate.DomainOfInfluenceTypes.Find(x => x != domainOfInfluenceType);
            if (domainOfInfluenceTypeToCopy == DomainOfInfluenceType.Unspecified)
            {
                continue;
            }

            var votingCardsToCopy = details.VotingCards.Where(x => hasMissingVotingCards && x.DomainOfInfluenceType == domainOfInfluenceTypeToCopy).ToList();
            var subTotalsToCopy = details.CountOfVotersInformationSubTotals.Where(x => hasMissingSubTotals && x.DomainOfInfluenceType == domainOfInfluenceTypeToCopy).ToList();

            foreach (var votingCard in votingCardsToCopy)
            {
                details.VotingCards.Add(new VotingCardResultDetail
                {
                    Channel = votingCard.Channel,
                    Valid = votingCard.Valid,
                    DomainOfInfluenceType = result.PoliticalBusiness.DomainOfInfluence.Type,
                    CountOfReceivedVotingCards = votingCard.CountOfReceivedVotingCards,
                });
            }

            foreach (var subTotal in subTotalsToCopy)
            {
                details.CountOfVotersInformationSubTotals.Add(new CountOfVotersInformationSubTotal
                {
                    CountOfVoters = subTotal.CountOfVoters,
                    Sex = subTotal.Sex,
                    VoterType = subTotal.VoterType,
                    DomainOfInfluenceType = result.PoliticalBusiness.DomainOfInfluence.Type,
                });
            }

            detailsToUpdate.Add(details);
        }

        await _ccDetailsRepo.UpdateRange(detailsToUpdate);
    }

    /// <summary>
    /// Syncs the ContestCountingCircleDetails (adds missing ones, removes odd, updates existing).
    /// </summary>
    /// <param name="contest">The contest with the existing CountingCircleDetails loaded.</param>
    /// <param name="doiCountingCircles">The counting circles of the doi of the contest.</param>
    /// <returns>A task which resolves to all removed details.</returns>
    private async Task<List<ContestCountingCircleDetails>> Sync(
        Contest contest,
        IEnumerable<DomainOfInfluenceCountingCircle> doiCountingCircles)
    {
        var existingValues = await _ccDetailsRepo.Query()
            .AsTracking()
            .Where(x => x.ContestId == contest.Id)
            .ToDictionaryAsync(x => x.CountingCircleId);

        var toAdd = new List<ContestCountingCircleDetails>();

        foreach (var cc in doiCountingCircles.DistinctBy(x => x.CountingCircleId).Select(x => x.CountingCircle))
        {
            if (existingValues.Remove(cc.Id, out var detail))
            {
                // eVoting can only be true, if it is enabled on the counting circle and the contest
                detail.EVoting = contest.EVoting && cc.EVoting;
                continue;
            }

            toAdd.Add(new ContestCountingCircleDetails
            {
                Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(contest.Id, cc.BasisCountingCircleId, contest.TestingPhaseEnded),
                ContestId = contest.Id,
                CountingCircleId = cc.Id,
                EVoting = contest.EVoting && cc.EVoting,
                ECounting = cc.ECounting,
            });
        }

        await _dataContext.SaveChangesAsync();

        var toRemove = existingValues.Values.ToList();

        await _ccDetailsRepo.DeleteRangeByKey(toRemove.Select(x => x.Id));
        await _ccDetailsRepo.CreateRange(toAdd);
        return toRemove;
    }

    private async Task<Dictionary<Guid, ContestCountingCircleDetails>> LoadCountingCircleDetailsByCountingCircleId(Guid contestId)
    {
        return await _ccDetailsRepo.Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Where(x => x.ContestId == contestId)
            .ToDictionaryAsync(x => x.CountingCircleId);
    }
}
