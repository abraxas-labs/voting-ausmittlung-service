// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class AggregatedContestCountingCircleDetailsBuilder
{
    private readonly DataContext _dataContext;
    private readonly IDbRepository<DataContext, ContestDetails> _contestDetailsRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;

    public AggregatedContestCountingCircleDetailsBuilder(
        IDbRepository<DataContext, ContestDetails> contestDetailsRepo,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        DataContext dataContext)
    {
        _contestDetailsRepo = contestDetailsRepo;
        _doiRepo = doiRepo;
        _dataContext = dataContext;
    }

    internal Task AdjustAggregatedDetails(
        ContestCountingCircleDetails countingCircleDetails,
        bool removeResults)
    {
        return AdjustAggregatedDetails(countingCircleDetails.ContestId, new[] { countingCircleDetails }, removeResults);
    }

    internal async Task AdjustAggregatedDetails(
        Guid contestId,
        IReadOnlyCollection<ContestCountingCircleDetails> countingCircleDetails,
        bool removeResults)
    {
        var deltaFactor = removeResults ? -1 : 1;

        var contestDetails = await _contestDetailsRepo.Query()
                                 .AsTracking()
                                 .AsSplitQuery()
                                 .Include(x => x.VotingCards)
                                 .Include(x => x.CountOfVotersInformationSubTotals)
                                 .FirstOrDefaultAsync(x => x.ContestId == contestId)
                             ?? new ContestDetails { ContestId = contestId };
        var doiDetailsByCcId = await GetDomainOfInfluenceDetailsByCountingCircleId(contestId, countingCircleDetails);

        foreach (var ccDetail in countingCircleDetails)
        {
            AdjustAggregatedDetails<ContestDetails, ContestCountOfVotersInformationSubTotal, ContestVotingCardResultDetail>(
                contestDetails,
                ccDetail,
                deltaFactor);

            foreach (var doiDetail in doiDetailsByCcId[ccDetail.CountingCircleId])
            {
                AdjustAggregatedDetails<ContestDomainOfInfluenceDetails, DomainOfInfluenceCountOfVotersInformationSubTotal, DomainOfInfluenceVotingCardResultDetail>(
                    doiDetail,
                    ccDetail,
                    deltaFactor);
            }
        }

        if (contestDetails.Id == Guid.Empty)
        {
            _dataContext.ContestDetails.Add(contestDetails);
        }

        await _dataContext.SaveChangesAsync();
    }

    private void AdjustAggregatedDetails<TAggregatedDetails, TCountOfVotersInformationSubTotal, TVotingCardResultDetail>(
        TAggregatedDetails aggregatedDetails,
        ContestCountingCircleDetails countingCircleDetails,
        int deltaFactor)
        where TAggregatedDetails : AggregatedContestCountingCircleDetails<TCountOfVotersInformationSubTotal, TVotingCardResultDetail>
        where TCountOfVotersInformationSubTotal : AggregatedCountOfVotersInformationSubTotal, new()
        where TVotingCardResultDetail : AggregatedVotingCardResultDetail, new()
    {
        aggregatedDetails.TotalCountOfVoters += countingCircleDetails.TotalCountOfVoters * deltaFactor;

        AdjustCountOfVotersInfo(
            aggregatedDetails.CountOfVotersInformationSubTotals,
            countingCircleDetails.CountOfVotersInformationSubTotals,
            deltaFactor);
        AdjustContestDetailsVotingCards(
            aggregatedDetails.VotingCards,
            countingCircleDetails.VotingCards,
            deltaFactor);
    }

    private void AdjustCountOfVotersInfo<TAggregatedCountOfVotersInformationSubTotal>(
        ICollection<TAggregatedCountOfVotersInformationSubTotal> aggregatedCountOfVotersInformationSubTotals,
        ICollection<CountOfVotersInformationSubTotal> countOfVotersInformationSubTotals,
        int deltaFactor)
        where TAggregatedCountOfVotersInformationSubTotal : AggregatedCountOfVotersInformationSubTotal, new()
    {
        var subTotalsBySexAndVoterType = aggregatedCountOfVotersInformationSubTotals.ToDictionary(x => (x.Sex, x.VoterType));
        foreach (var countOfVotersInfoSubTotal in countOfVotersInformationSubTotals)
        {
            subTotalsBySexAndVoterType.TryGetValue((countOfVotersInfoSubTotal.Sex, countOfVotersInfoSubTotal.VoterType), out var matchingSubTotal);
            if (matchingSubTotal == null)
            {
                matchingSubTotal = new()
                {
                    VoterType = countOfVotersInfoSubTotal.VoterType,
                    Sex = countOfVotersInfoSubTotal.Sex,
                };

                aggregatedCountOfVotersInformationSubTotals.Add(matchingSubTotal);
            }

            matchingSubTotal.CountOfVoters += countOfVotersInfoSubTotal.CountOfVoters.GetValueOrDefault() * deltaFactor;
        }
    }

    private void AdjustContestDetailsVotingCards<TVotingCardResultDetail>(
        ICollection<TVotingCardResultDetail> aggregatedVotingCards,
        ICollection<VotingCardResultDetail> votingCards,
        int deltaFactor)
        where TVotingCardResultDetail : AggregatedVotingCardResultDetail, new()
    {
        var votingCardsByUniqueKey = aggregatedVotingCards.ToDictionary(x => (x.Channel, x.Valid, x.DomainOfInfluenceType));
        foreach (var votingCard in votingCards)
        {
            votingCardsByUniqueKey.TryGetValue((votingCard.Channel, votingCard.Valid, votingCard.DomainOfInfluenceType), out var matchingVotingCard);
            if (matchingVotingCard == null)
            {
                matchingVotingCard = new()
                {
                    Channel = votingCard.Channel,
                    Valid = votingCard.Valid,
                    DomainOfInfluenceType = votingCard.DomainOfInfluenceType,
                };

                aggregatedVotingCards.Add(matchingVotingCard);
            }

            matchingVotingCard.CountOfReceivedVotingCards += votingCard.CountOfReceivedVotingCards.GetValueOrDefault() * deltaFactor;
        }
    }

    private async Task<Dictionary<Guid, List<ContestDomainOfInfluenceDetails>>> GetDomainOfInfluenceDetailsByCountingCircleId(
        Guid contestId,
        IEnumerable<ContestCountingCircleDetails> ccDetails)
    {
        var ccIds = ccDetails.Select(x => x.CountingCircleId).ToList();

        var dois = await _doiRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(doi => doi.CountingCircles.Where(doiCc => ccIds.Contains(doiCc.CountingCircleId)))
            .Include(doi => doi.Details)
            .ThenInclude(d => d!.CountOfVotersInformationSubTotals)
            .Include(doi => doi.Details)
            .ThenInclude(d => d!.VotingCards)
            .Where(doi => doi.SnapshotContestId == contestId)
            .ToListAsync();

        foreach (var doi in dois)
        {
            if (doi.Details != null)
            {
                continue;
            }

            doi.Details = new()
            {
                ContestId = contestId,
                DomainOfInfluenceId = doi.Id,
            };
        }

        return dois.SelectMany(doi => doi.CountingCircles)
            .GroupBy(x => x.CountingCircleId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.DomainOfInfluence.Details!).ToList());
    }
}
