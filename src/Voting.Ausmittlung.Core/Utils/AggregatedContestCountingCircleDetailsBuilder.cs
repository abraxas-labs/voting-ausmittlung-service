// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
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

        var contestDetails = await GetContestDetails(contestId);
        var doiDetailsByCcId = await GetDomainOfInfluenceDetailsByCountingCircleId(contestId, countingCircleDetails);

        foreach (var ccDetail in countingCircleDetails)
        {
            AggregatedContestCountingCircleDetailsUtil.AdjustAggregatedDetails<ContestDetails, ContestCountOfVotersInformationSubTotal, ContestVotingCardResultDetail>(
                contestDetails,
                ccDetail,
                deltaFactor);

            foreach (var doiDetail in doiDetailsByCcId[ccDetail.CountingCircleId])
            {
                AggregatedContestCountingCircleDetailsUtil.AdjustAggregatedDetails<ContestDomainOfInfluenceDetails, DomainOfInfluenceCountOfVotersInformationSubTotal, DomainOfInfluenceVotingCardResultDetail>(
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

    internal async Task AdjustAggregatedVotingCards(
        Guid contestId,
        IReadOnlyCollection<ContestCountingCircleDetails> countingCircleDetails,
        bool removeResults)
    {
        var deltaFactor = removeResults ? -1 : 1;

        var contestDetails = await GetContestDetails(contestId);
        var doiDetailsByCcId = await GetDomainOfInfluenceDetailsByCountingCircleId(contestId, countingCircleDetails);

        foreach (var ccDetail in countingCircleDetails)
        {
            AggregatedContestCountingCircleDetailsUtil.AdjustContestDetailsVotingCards(
                contestDetails.VotingCards,
                ccDetail.VotingCards,
                deltaFactor);

            foreach (var doiDetail in doiDetailsByCcId[ccDetail.CountingCircleId])
            {
                AggregatedContestCountingCircleDetailsUtil.AdjustContestDetailsVotingCards(
                    doiDetail.VotingCards,
                    ccDetail.VotingCards,
                    deltaFactor);
            }
        }

        if (contestDetails.Id == Guid.Empty)
        {
            _dataContext.ContestDetails.Add(contestDetails);
        }

        await _dataContext.SaveChangesAsync();
    }

    private async Task<ContestDetails> GetContestDetails(Guid contestId)
    {
        return await _contestDetailsRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(x => x.ContestId == contestId)
            ?? new ContestDetails { ContestId = contestId };
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

        return dois.SelectMany(doi => doi.CountingCircles.DistinctBy(x => x.CountingCircleId))
            .GroupBy(x => x.CountingCircleId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.DomainOfInfluence.Details!).ToList());
    }
}
