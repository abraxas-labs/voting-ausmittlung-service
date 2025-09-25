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
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class AggregatedContestCountingCircleDetailsBuilder
{
    private readonly DataContext _dataContext;
    private readonly IDbRepository<DataContext, ContestDetails> _contestDetailsRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleCcResultsRepo;

    public AggregatedContestCountingCircleDetailsBuilder(
        IDbRepository<DataContext, ContestDetails> contestDetailsRepo,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleCcResultsRepo,
        DataContext dataContext)
    {
        _contestDetailsRepo = contestDetailsRepo;
        _doiRepo = doiRepo;
        _simpleCcResultsRepo = simpleCcResultsRepo;
        _dataContext = dataContext;
    }

    internal async Task AdjustAggregatedDetails(Guid resultId, bool removeResults)
    {
        var result = await _simpleCcResultsRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle!.ContestDetails)
            .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.CountingCircle!.ContestDetails)
            .ThenInclude(x => x.VotingCards)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(resultId);

        // If any other result of the counting circle is already done,
        // then modifying this result has no effect.
        var hasNoEffect = await _simpleCcResultsRepo.Query()
            .AnyAsync(r =>
                r.Id != resultId
                && r.CountingCircleId == result.CountingCircleId
                && r.State >= CountingCircleResultState.SubmissionDone);
        if (hasNoEffect)
        {
            return;
        }

        var details = result.CountingCircle!.ContestDetails.Single();
        await AdjustAggregatedDetails(
            details,
            removeResults,
            needAffectedDetailsCheck: false);
    }

    internal Task AdjustAggregatedDetails(
        ContestCountingCircleDetails countingCircleDetails,
        bool removeResults,
        bool needAffectedDetailsCheck = true)
    {
        return AdjustAggregatedDetails(
            countingCircleDetails.ContestId,
            [countingCircleDetails],
            removeResults,
            needAffectedDetailsCheck);
    }

    /// <summary>
    /// Adjusts the aggregated counting circle details.
    /// By default, skips results that are in a state that should have already been adjusted.
    /// </summary>
    /// <param name="contestId">The contest id.</param>
    /// <param name="countingCircleDetails">The counting circle details.</param>
    /// <param name="removeResults">True to remove the results, false to add them to the aggregated details.</param>
    /// <param name="needAffectedDetailsCheck">
    /// If true (the default), check whether the counting circle results are in a state that "matters".
    /// If false, add/remove the counting circle details regardless of their result's states.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal async Task AdjustAggregatedDetails(
        Guid contestId,
        IReadOnlyCollection<ContestCountingCircleDetails> countingCircleDetails,
        bool removeResults,
        bool needAffectedDetailsCheck = true)
    {
        var deltaFactor = removeResults ? -1 : 1;
        countingCircleDetails = needAffectedDetailsCheck
            ? await RemoveNotAffectedDetails(contestId, countingCircleDetails)
            : countingCircleDetails;

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
        countingCircleDetails = await RemoveNotAffectedDetails(contestId, countingCircleDetails);

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

    private async Task<IReadOnlyCollection<ContestCountingCircleDetails>> RemoveNotAffectedDetails(
        Guid contestId,
        IReadOnlyCollection<ContestCountingCircleDetails> countingCircleDetails)
    {
        // The counting circle details are only affected if the result submission is "done"
        var countingCircleIds = countingCircleDetails
            .Select(d => d.CountingCircleId)
            .ToHashSet();
        var affectedCcIds = await _simpleCcResultsRepo.Query()
            .Where(r => countingCircleIds.Contains(r.CountingCircleId)
                && r.PoliticalBusiness!.ContestId == contestId
                && r.State >= CountingCircleResultState.SubmissionDone)
            .Select(x => x.CountingCircleId)
            .ToHashSetAsync();
        return countingCircleDetails
            .Where(d => affectedCcIds.Contains(d.CountingCircleId))
            .ToList();
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

            doi.Details = new ContestDomainOfInfluenceDetails
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
