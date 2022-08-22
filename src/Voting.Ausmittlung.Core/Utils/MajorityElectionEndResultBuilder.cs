// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class MajorityElectionEndResultBuilder
{
    private readonly MajorityElectionResultRepo _resultRepo;
    private readonly MajorityElectionEndResultRepo _endResultRepo;
    private readonly MajorityElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly MajorityElectionStrategyFactory _calculationStrategyFactory;
    private readonly IDbRepository<DataContext, MajorityElectionWriteInMapping> _majorityElectionWriteInsRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> _secondaryMajorityElectionWriteInsRepo;
    private readonly DataContext _dbContext;

    public MajorityElectionEndResultBuilder(
        MajorityElectionResultRepo resultRepo,
        MajorityElectionEndResultRepo endResultRepo,
        MajorityElectionCandidateEndResultBuilder candidateEndResultBuilder,
        MajorityElectionStrategyFactory calculationStrategyFactory,
        DataContext dbContext,
        IDbRepository<DataContext, MajorityElectionWriteInMapping> majorityElectionWriteInsRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> secondaryMajorityElectionWriteInsRepo)
    {
        _resultRepo = resultRepo;
        _endResultRepo = endResultRepo;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _calculationStrategyFactory = calculationStrategyFactory;
        _dbContext = dbContext;
        _majorityElectionWriteInsRepo = majorityElectionWriteInsRepo;
        _secondaryMajorityElectionWriteInsRepo = secondaryMajorityElectionWriteInsRepo;
    }

    internal async Task ResetAllResults(Guid contestId, VotingDataSource dataSource)
    {
        var endResults = await _endResultRepo.ListWithResultsByContestIdAsTracked(contestId);

        foreach (var endResult in endResults)
        {
            endResult.ResetCalculation();
            endResult.ResetAllSubTotals(dataSource, true);

            foreach (var result in endResult.MajorityElection.Results)
            {
                result.ResetAllSubTotals(dataSource, true);
            }
        }

        if (dataSource == VotingDataSource.EVoting)
        {
            var primaryIdsToDelete = await _majorityElectionWriteInsRepo.Query()
                .Where(x => x.Result.MajorityElection.ContestId == contestId)
                .Select(x => x.Id)
                .ToListAsync();
            await _majorityElectionWriteInsRepo.DeleteRangeByKey(primaryIdsToDelete);

            var secondaryIdsToDelete = await _secondaryMajorityElectionWriteInsRepo.Query()
                .Where(x => x.Result.SecondaryMajorityElection.PrimaryMajorityElection.ContestId == contestId)
                .Select(x => x.Id)
                .ToListAsync();
            await _secondaryMajorityElectionWriteInsRepo.DeleteRangeByKey(secondaryIdsToDelete);
        }

        await _dbContext.SaveChangesAsync();
    }

    internal async Task RecalculateForLotDecisions(
        Guid majorityElectionId,
        IEnumerable<ElectionEndResultLotDecision> lotDecisions)
    {
        var majorityElectionEndResult = await _endResultRepo.GetByMajorityElectionIdAsTracked(majorityElectionId)
                                        ?? throw new EntityNotFoundException(majorityElectionId);

        _candidateEndResultBuilder.UpdateCandidateEndResultRanksByLotDecisions(majorityElectionEndResult, lotDecisions);
        var strategy = _calculationStrategyFactory.GetMajorityElectionMandateAlgorithmStrategy(
            majorityElectionEndResult.MajorityElection.DomainOfInfluence.CantonDefaults.MajorityElectionAbsoluteMajorityAlgorithm,
            majorityElectionEndResult.MajorityElection.MandateAlgorithm);
        strategy.RecalculateCandidateEndResultStates(majorityElectionEndResult);
        majorityElectionEndResult.Finalized = false;

        await _dbContext.SaveChangesAsync();
    }

    internal async Task AdjustEndResult(Guid resultId, bool removeResults)
    {
        var deltaFactor = removeResults ? -1 : 1;

        var result = await _resultRepo
                         .Query()
                         .AsSplitQuery()
                         .Include(x => x.CandidateResults)
                         .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                         .FirstOrDefaultAsync(x => x.Id == resultId)
                     ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), resultId);

        var endResult = await _endResultRepo.GetByMajorityElectionIdAsTracked(result.MajorityElectionId)
                        ?? throw new EntityNotFoundException(nameof(MajorityElectionEndResult), resultId);

        endResult.CountOfDoneCountingCircles += deltaFactor;
        endResult.TotalCountOfVoters += result.TotalCountOfVoters * deltaFactor;
        endResult.Finalized = false;

        PoliticalBusinessCountOfVotersUtils.AdjustCountOfVoters(
            endResult.CountOfVoters,
            result.CountOfVoters,
            endResult.TotalCountOfVoters,
            deltaFactor);

        endResult.ResetCalculation();
        endResult.ForEachSubTotal(result, (endResultSubTotal, resultSubTotal) => AdjustEndResult(endResultSubTotal, resultSubTotal, deltaFactor));

        _candidateEndResultBuilder.AdjustCandidateEndResults(
            endResult.CandidateEndResults,
            result.CandidateResults,
            deltaFactor,
            endResult.AllCountingCirclesDone);

        AdjustSecondaryEndResults(endResult, result, deltaFactor);

        var strategy = _calculationStrategyFactory.GetMajorityElectionMandateAlgorithmStrategy(
            endResult.MajorityElection.DomainOfInfluence.CantonDefaults.MajorityElectionAbsoluteMajorityAlgorithm,
            endResult.MajorityElection.MandateAlgorithm);
        strategy.RecalculateCandidateEndResultStates(endResult);

        await _dbContext.SaveChangesAsync();
    }

    private void AdjustEndResult(
        MajorityElectionResultSubTotal endResultSubTotal,
        IMajorityElectionResultSubTotal<int> resultSubTotal,
        int deltaFactor)
    {
        endResultSubTotal.IndividualVoteCount += resultSubTotal.IndividualVoteCount * deltaFactor;
        endResultSubTotal.EmptyVoteCount += resultSubTotal.EmptyVoteCount * deltaFactor;
        endResultSubTotal.InvalidVoteCount += resultSubTotal.InvalidVoteCount * deltaFactor;
        endResultSubTotal.TotalCandidateVoteCountExclIndividual += resultSubTotal.TotalCandidateVoteCountExclIndividual * deltaFactor;
    }

    private void AdjustSecondaryEndResults(
        MajorityElectionEndResult endResult,
        MajorityElectionResult result,
        int deltaFactor)
    {
        endResult.SecondaryMajorityElectionEndResults.MatchAndExec(
                x => x.SecondaryMajorityElectionId,
                result.SecondaryMajorityElectionResults,
                x => x.SecondaryMajorityElectionId,
                (secEndResult, secResult) => AdjustSecondaryEndResults(secEndResult, secResult, endResult.AllCountingCirclesDone, deltaFactor));
    }

    private void AdjustSecondaryEndResults(
        SecondaryMajorityElectionEndResult endResult,
        SecondaryMajorityElectionResult result,
        bool allCountingCirclesDone,
        int deltaFactor)
    {
        endResult.ForEachSubTotal(result, (endResultSubTotal, resultSubTotal) => AdjustEndResult(endResultSubTotal, resultSubTotal, deltaFactor));

        _candidateEndResultBuilder.AdjustCandidateEndResults(
            endResult.CandidateEndResults,
            result.CandidateResults,
            deltaFactor,
            allCountingCirclesDone);
    }
}
