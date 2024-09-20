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
    private readonly DataContext _dataContext;

    public ContestCountingCircleDetailsBuilder(
        DomainOfInfluenceCountingCircleRepo doiCountingCirclesRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        AggregatedContestCountingCircleDetailsBuilder aggregatedContestCountingCircleDetailsBuilder,
        DataContext dataContext)
    {
        _doiCountingCirclesRepo = doiCountingCirclesRepo;
        _contestRepo = contestRepo;
        _ccDetailsRepo = ccDetailsRepo;
        _countingCircleRepo = countingCircleRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _simpleResultRepo = simpleResultRepo;
        _aggregatedContestCountingCircleDetailsBuilder = aggregatedContestCountingCircleDetailsBuilder;
        _dataContext = dataContext;
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

    internal async Task ResetEVotingVotingCards(Guid contestId)
    {
        var ccDetails = await _ccDetailsRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards.Where(vc => vc.Channel == VotingChannel.EVoting))
            .Where(x => x.ContestId == contestId && x.VotingCards.Any(vc => vc.Channel == VotingChannel.EVoting))
            .ToListAsync();

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedVotingCards(contestId, ccDetails, true);

        foreach (var votingCard in ccDetails.SelectMany(x => x.VotingCards))
        {
            votingCard.CountOfReceivedVotingCards = 0;
        }

        await _ccDetailsRepo.UpdateRange(ccDetails);
    }

    internal async Task ResetConventionalVotingCards(Guid contestId, Guid basisCountingCircleId)
    {
        var ccDetails = await _ccDetailsRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards.Where(vc => vc.Channel != VotingChannel.EVoting))
            .Include(x => x.CountingCircle)
            .Where(x => x.ContestId == contestId && x.CountingCircle.BasisCountingCircleId == basisCountingCircleId && x.VotingCards.Any(vc => vc.Channel != VotingChannel.EVoting))
            .SingleOrDefaultAsync();

        if (ccDetails == null)
        {
            return;
        }

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedVotingCards(contestId, new[] { ccDetails }, true);

        foreach (var votingCard in ccDetails.VotingCards)
        {
            votingCard.CountOfReceivedVotingCards = null;
        }

        // don't want to save counting circle again
        ccDetails.CountingCircle = null!;

        await _ccDetailsRepo.Update(ccDetails);
    }

    internal async Task CreateMissingVotingCardsInElectorate(Guid politicalBusinessId, Guid contestId, Guid domainOfInfluenceId)
    {
        var simpleResults = await _simpleResultRepo.Query()
            .Include(x => x.PoliticalBusiness!.Contest.DomainOfInfluence)
            .Include(x => x.PoliticalBusiness!.DomainOfInfluence)
            .Include(x => x.CountingCircle)
            .Where(x => x.PoliticalBusinessId == politicalBusinessId)
            .ToListAsync();

        var detailsByCountingCircleId = await _ccDetailsRepo.Query()
            .Include(x => x.VotingCards)
            .Where(x => x.ContestId == contestId)
            .ToDictionaryAsync(x => x.CountingCircleId);

        var countingCirclesByBasisCountingCircleId = await _countingCircleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Electorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
            .Include(x => x.ContestElectorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
            .Where(x => x.SnapshotContestId == contestId)
            .ToDictionaryAsync(x => x.BasisCountingCircleId);

        var domainOfInfluence = await _domainOfInfluenceRepo.GetByKey(domainOfInfluenceId)
            ?? throw new EntityNotFoundException(nameof(DomainOfInfluence), domainOfInfluenceId);

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

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedVotingCards(contestId, detailsByCountingCircleId.Values, true);

        var detailsToUpdate = new List<ContestCountingCircleDetails>();
        foreach (var result in simpleResults)
        {
            // details contain already the desired voting cards for this domain of influence type or voting cards are empty
            if (!detailsByCountingCircleId.TryGetValue(result.CountingCircleId, out var details) ||
                details.VotingCards.Count == 0 ||
                details.VotingCards.Any(x => x.DomainOfInfluenceType == result.PoliticalBusiness!.DomainOfInfluence.Type) ||
                !countingCirclesByBasisCountingCircleId.TryGetValue(result.CountingCircle!.BasisCountingCircleId, out var countingCircle) ||
                !resultsByCountingCircleId.TryGetValue(result.CountingCircleId, out var results))
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
            var domainOfInfluenceTypeToCopy = electorate.DomainOfInfluenceTypes.FirstOrDefault(x => x != domainOfInfluenceType);
            if (domainOfInfluenceTypeToCopy == DomainOfInfluenceType.Unspecified)
            {
                continue;
            }

            var votingCardsToCopy = details.VotingCards.Where(x => x.DomainOfInfluenceType == domainOfInfluenceTypeToCopy).ToList();

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

            detailsToUpdate.Add(details);
        }

        await _ccDetailsRepo.UpdateRange(detailsToUpdate);
        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedVotingCards(contestId, detailsByCountingCircleId.Values, false);
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

        foreach (var cc in doiCountingCircles.Select(x => x.CountingCircle))
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
            });
        }

        await _dataContext.SaveChangesAsync();

        var toRemove = existingValues.Values.ToList();

        await _ccDetailsRepo.DeleteRangeByKey(toRemove.Select(x => x.Id));
        await _ccDetailsRepo.CreateRange(toAdd);
        return toRemove;
    }
}
