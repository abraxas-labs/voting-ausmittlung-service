// (c) Copyright by Abraxas Informatik AG
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
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;
    private readonly IDbRepository<DataContext, MajorityElectionWriteInBallot> _majorityElectionWriteInBallotsRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionWriteInBallot> _secondaryMajorityElectionWriteInBallotsRepo;
    private readonly DataContext _dbContext;

    public MajorityElectionEndResultBuilder(
        MajorityElectionResultRepo resultRepo,
        MajorityElectionEndResultRepo endResultRepo,
        MajorityElectionCandidateEndResultBuilder candidateEndResultBuilder,
        MajorityElectionStrategyFactory calculationStrategyFactory,
        DataContext dbContext,
        IDbRepository<DataContext, MajorityElectionWriteInMapping> majorityElectionWriteInsRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> secondaryMajorityElectionWriteInsRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo,
        IDbRepository<DataContext, MajorityElectionWriteInBallot> majorityElectionWriteInBallotsRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionWriteInBallot> secondaryMajorityElectionWriteInBallotsRepo)
    {
        _resultRepo = resultRepo;
        _endResultRepo = endResultRepo;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _calculationStrategyFactory = calculationStrategyFactory;
        _dbContext = dbContext;
        _majorityElectionWriteInsRepo = majorityElectionWriteInsRepo;
        _secondaryMajorityElectionWriteInsRepo = secondaryMajorityElectionWriteInsRepo;
        _simpleResultRepo = simpleResultRepo;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
        _majorityElectionWriteInBallotsRepo = majorityElectionWriteInBallotsRepo;
        _secondaryMajorityElectionWriteInBallotsRepo = secondaryMajorityElectionWriteInBallotsRepo;
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
                await ResetSimpleResult(result, dataSource);
            }
        }

        if (dataSource is VotingDataSource.EVoting or VotingDataSource.ECounting)
        {
            var importType = dataSource.GetImportType();
            await _majorityElectionWriteInsRepo.Query()
                .Where(x => x.Import!.ContestId == contestId && x.Import!.ImportType == importType)
                .ExecuteDeleteAsync();
            await _majorityElectionWriteInBallotsRepo.Query()
                .Where(x => x.Import!.ContestId == contestId && x.Import!.ImportType == importType)
                .ExecuteDeleteAsync();
            await _secondaryMajorityElectionWriteInsRepo.Query()
                .Where(x => x.Import!.ContestId == contestId && x.Import!.ImportType == importType)
                .ExecuteDeleteAsync();
            await _secondaryMajorityElectionWriteInBallotsRepo.Query()
                .Where(x => x.Import!.ContestId == contestId && x.Import!.ImportType == importType)
                .ExecuteDeleteAsync();
        }

        await _dbContext.SaveChangesAsync();
    }

    internal async Task SetPrimaryLotDecisions(Guid majorityElectionId, IEnumerable<ElectionEndResultLotDecision> lotDecisions)
    {
        var majorityElectionEndResult = await _endResultRepo.GetByMajorityElectionIdAsTracked(majorityElectionId)
                                ?? throw new EntityNotFoundException(majorityElectionId);

        var primaryCandidateIds = majorityElectionEndResult.MajorityElection.MajorityElectionCandidates.Select(c => c.Id).ToList();
        var primaryLotDecisions = lotDecisions.Where(l => primaryCandidateIds.Contains(l.CandidateId));
        _candidateEndResultBuilder.UpdateCandidateEndResultRanksByLotDecisions(majorityElectionEndResult, primaryLotDecisions);

        var strategy = _calculationStrategyFactory.GetMajorityElectionMandateAlgorithmStrategy(
            majorityElectionEndResult.MajorityElection.Contest.CantonDefaults.MajorityElectionAbsoluteMajorityAlgorithm,
            majorityElectionEndResult.MajorityElection.MandateAlgorithm);
        strategy.RecalculatePrimaryCandidateEndResultStates(majorityElectionEndResult);

        // Old events could contain secondary lot decisions.
        var secondaryCandidateIds = majorityElectionEndResult.MajorityElection.SecondaryMajorityElections
            .SelectMany(sme => sme.Candidates)
            .Select(c => c.Id)
            .ToList();
        var secondaryLotDecisions = lotDecisions.Where(l => secondaryCandidateIds.Contains(l.CandidateId)).ToList();
        if (secondaryLotDecisions.Count > 0)
        {
            _candidateEndResultBuilder.UpdateCandidateEndResultRanksByLotDecisions(majorityElectionEndResult, secondaryLotDecisions);
            strategy.RecalculateSecondaryCandidateEndResultStates(majorityElectionEndResult);
        }

        await _dbContext.SaveChangesAsync();
    }

    internal async Task SetSecondaryLotDecisions(Guid majorityElectionId, IEnumerable<ElectionEndResultLotDecision> lotDecisions)
    {
        var majorityElectionEndResult = await _endResultRepo.GetByMajorityElectionIdAsTracked(majorityElectionId)
            ?? throw new EntityNotFoundException(majorityElectionId);
        _candidateEndResultBuilder.UpdateCandidateEndResultRanksByLotDecisions(majorityElectionEndResult, lotDecisions);

        var strategy = _calculationStrategyFactory.GetMajorityElectionMandateAlgorithmStrategy(
            majorityElectionEndResult.MajorityElection.Contest.CantonDefaults.MajorityElectionAbsoluteMajorityAlgorithm,
            majorityElectionEndResult.MajorityElection.MandateAlgorithm);
        strategy.RecalculateSecondaryCandidateEndResultStates(majorityElectionEndResult);

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
            .Include(x => x.CandidateResults)
            .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), resultId);

        var countingCircleDetails = result.CountingCircle.ContestDetails.FirstOrDefault()
            ?? throw new EntityNotFoundException(nameof(ContestDetails), resultId);

        var endResult = await _endResultRepo.GetByMajorityElectionIdAsTracked(result.MajorityElectionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionEndResult), resultId);

        var simplePb = await _simplePoliticalBusinessRepo.Query()
            .AsSplitQuery()
            .AsTracking()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Contest.CantonDefaults)
            .FirstOrDefaultAsync(x => x.Id == endResult.MajorityElectionId)
            ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), endResult.MajorityElectionId);

        endResult.CountOfDoneCountingCircles += deltaFactor;

        var implicitFinalized = simplePb.Contest.CantonDefaults.EndResultFinalizeDisabled && endResult.AllCountingCirclesDone;
        endResult.Finalized = implicitFinalized;
        simplePb.EndResultFinalized = implicitFinalized;

        EndResultContestDetailsUtils.AdjustEndResultContestDetails<
            MajorityElectionEndResult,
            MajorityElectionEndResultCountOfVotersInformationSubTotal,
            MajorityElectionEndResultVotingCardDetail>(
                endResult,
                countingCircleDetails,
                simplePb.DomainOfInfluence,
                deltaFactor);

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
            endResult.MajorityElection.Contest.CantonDefaults.MajorityElectionAbsoluteMajorityAlgorithm,
            endResult.MajorityElection.MandateAlgorithm);
        strategy.RecalculatePrimaryCandidateEndResultStates(endResult);

        await _dbContext.SaveChangesAsync();
    }

    private void AdjustEndResult(
        MajorityElectionResultSubTotal endResultSubTotal,
        IMajorityElectionResultSubTotal<int> resultSubTotal,
        int deltaFactor)
    {
        endResultSubTotal.IndividualVoteCount += resultSubTotal.IndividualVoteCount * deltaFactor;
        endResultSubTotal.EmptyVoteCountExclWriteIns += resultSubTotal.EmptyVoteCountExclWriteIns * deltaFactor;
        endResultSubTotal.EmptyVoteCountWriteIns += resultSubTotal.EmptyVoteCountWriteIns * deltaFactor;
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

    private async Task ResetSimpleResult(MajorityElectionResult result, VotingDataSource dataSource)
    {
        switch (dataSource)
        {
            case VotingDataSource.EVoting:
                await _simpleResultRepo.Query()
                    .Where(x => x.Id == result.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(
                        y => y.CountOfElectionsWithUnmappedEVotingWriteIns,
                        0));
                break;
            case VotingDataSource.ECounting:
                await _simpleResultRepo.Query()
                    .Where(x => x.Id == result.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(
                        y => y.CountOfElectionsWithUnmappedECountingWriteIns,
                        0));
                break;
        }
    }
}
