// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils.ProportionalElectionStrategy;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionEndResultBuilder
{
    private readonly ProportionalElectionEndResultRepo _endResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;
    private readonly DataContext _dbContext;
    private readonly ProportionalElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly ILogger<ProportionalElectionEndResultBuilder> _logger;

    public ProportionalElectionEndResultBuilder(
        ProportionalElectionEndResultRepo endResultRepo,
        IDbRepository<DataContext, ProportionalElectionResult> resultRepo,
        DataContext dbContext,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder,
        ILogger<ProportionalElectionEndResultBuilder> logger,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo)
    {
        _endResultRepo = endResultRepo;
        _resultRepo = resultRepo;
        _dbContext = dbContext;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _logger = logger;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
    }

    internal async Task ResetAllResults(Guid contestId, VotingDataSource dataSource)
    {
        var endResults = await _endResultRepo.ListWithResultsByContestIdAsTracked(contestId);

        foreach (var endResult in endResults)
        {
            ResetCalculations(endResult);
            endResult.ResetAllSubTotals(dataSource, true);

            foreach (var result in endResult.ProportionalElection.Results)
            {
                result.ResetAllSubTotals(dataSource, true);
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    internal async Task AdjustEndResult(Guid resultId, bool removeResults)
    {
        var deltaFactor = removeResults ? -1 : 1;

        var result = await _resultRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.VotingCards)
            .Include(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.ListResults)
            .ThenInclude(x => x.CandidateResults)
            .ThenInclude(x => x.VoteSources)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionResult), resultId);

        var countingCircleDetails = result.CountingCircle.ContestDetails.FirstOrDefault()
            ?? throw new EntityNotFoundException(nameof(ContestDetails), resultId);

        var endResult = await _endResultRepo.GetByProportionalElectionIdAsTracked(result.ProportionalElectionId)
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionEndResult), resultId);

        var simpleEndResult = await _simplePoliticalBusinessRepo.Query()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == endResult.ProportionalElectionId)
            ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), endResult.ProportionalElectionId);

        endResult.CountOfDoneCountingCircles += deltaFactor;
        endResult.Finalized = false;
        endResult.ManualEndResultRequired = false;
        simpleEndResult.EndResultFinalized = false;

        EndResultContestDetailsUtils.AdjustEndResultContestDetails<
            ProportionalElectionEndResult,
            ProportionalElectionEndResultCountOfVotersInformationSubTotal,
            ProportionalElectionEndResultVotingCardDetail>(
                endResult,
                countingCircleDetails,
                deltaFactor);

        PoliticalBusinessCountOfVotersUtils.AdjustCountOfVoters(
            endResult.CountOfVoters,
            result.CountOfVoters,
            endResult.TotalCountOfVoters,
            deltaFactor);

        ResetCalculations(endResult);

        endResult.ForEachSubTotal(result, (endResultSubTotal, resultSubTotal) => AdjustEndResult(endResultSubTotal, resultSubTotal, deltaFactor));
        AdjustListAndCandidateEndResults(endResult, result, deltaFactor, endResult.AllCountingCirclesDone);

        ProportionalElectionHagenbachBischoffStrategy.RecalculateNumberOfMandatesForLists(endResult);

        foreach (var listEndResult in endResult.ListEndResults)
        {
            _candidateEndResultBuilder.RecalculateLotDecisionRequired(listEndResult);
        }

        if (!endResult.ManualEndResultRequired)
        {
            _candidateEndResultBuilder.RecalculateCandidateEndResultStates(endResult);
        }
        else
        {
            _logger.LogWarning("Hagenbach Bischoff could not distribute all number of mandates. Manual end result required for election {ProportionalElectionId}", endResult.ProportionalElectionId);
        }

        await _dbContext.SaveChangesAsync();
    }

    private void ResetCalculations(ProportionalElectionEndResult endResult)
    {
        endResult.ResetCalculation();
    }

    private void AdjustListAndCandidateEndResults(
        ProportionalElectionEndResult endResult,
        ProportionalElectionResult result,
        int deltaFactor,
        bool allCountingCirclesDone)
    {
        endResult.ListEndResults.MatchAndExec(
            x => x.ListId,
            result.ListResults,
            x => x.ListId,
            (listEndResult, listResult) =>
            {
                listEndResult.ForEachSubTotal(listResult, (endResultSubTotal, listResultSubTotal) => AdjustListEndResult(endResultSubTotal, listResultSubTotal, deltaFactor));

                // has open lot decisions is calculated when the candidate end result states are set
                listEndResult.HasOpenRequiredLotDecisions = false;

                _candidateEndResultBuilder.AdjustCandidateEndResults(
                    listEndResult.CandidateEndResults,
                    listResult.CandidateResults,
                    deltaFactor,
                    allCountingCirclesDone);
            });
    }

    private void AdjustEndResult(
        ProportionalElectionResultSubTotal endResultSubTotal,
        IProportionalElectionResultTotal resultSubTotal,
        int deltaFactor)
    {
        endResultSubTotal.TotalCountOfUnmodifiedLists += resultSubTotal.TotalCountOfUnmodifiedLists * deltaFactor;
        endResultSubTotal.TotalCountOfModifiedLists += resultSubTotal.TotalCountOfModifiedLists * deltaFactor;
        endResultSubTotal.TotalCountOfListsWithoutParty += resultSubTotal.TotalCountOfListsWithoutParty * deltaFactor;
        endResultSubTotal.TotalCountOfBlankRowsOnListsWithoutParty += resultSubTotal.TotalCountOfBlankRowsOnListsWithoutParty * deltaFactor;
    }

    private void AdjustListEndResult(
        ProportionalElectionListResultSubTotal endResultSubTotal,
        IProportionalElectionListResultTotal resultSubTotal,
        int deltaFactor)
    {
        endResultSubTotal.UnmodifiedListsCount += resultSubTotal.UnmodifiedListsCount * deltaFactor;
        endResultSubTotal.UnmodifiedListVotesCount += resultSubTotal.UnmodifiedListVotesCount * deltaFactor;
        endResultSubTotal.ModifiedListVotesCount += resultSubTotal.ModifiedListVotesCount * deltaFactor;
        endResultSubTotal.ListVotesCountOnOtherLists += resultSubTotal.ListVotesCountOnOtherLists * deltaFactor;
        endResultSubTotal.ModifiedListsCount += resultSubTotal.ModifiedListsCount * deltaFactor;
        endResultSubTotal.UnmodifiedListBlankRowsCount += resultSubTotal.UnmodifiedListBlankRowsCount * deltaFactor;
        endResultSubTotal.ModifiedListBlankRowsCount += resultSubTotal.ModifiedListBlankRowsCount * deltaFactor;
    }
}
