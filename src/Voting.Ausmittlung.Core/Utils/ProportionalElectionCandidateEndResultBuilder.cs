// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionCandidateEndResultBuilder : ElectionCandidateEndResultBuilder
{
    private const int DefaultRank = 1;

    private readonly ProportionalElectionCandidateRepo _candidateRepo;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;
    private readonly DataContext _dataContext;

    public ProportionalElectionCandidateEndResultBuilder(
        ProportionalElectionCandidateRepo candidateRepo,
        DataContext dataContext,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo)
    {
        _candidateRepo = candidateRepo;
        _dataContext = dataContext;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
    }

    internal async Task Initialize(Guid proportionalElectionCandidateId)
    {
        var candidate = await _candidateRepo.GetWithEndResultsAsTracked(proportionalElectionCandidateId)
            ?? throw new EntityNotFoundException(proportionalElectionCandidateId);

        AddMissingEndResultToCandidate(candidate);
        await _dataContext.SaveChangesAsync();
    }

    internal void AddMissingCandidateEndResults(
        ProportionalElectionListEndResult listEndResult,
        IEnumerable<ProportionalElectionCandidate> candidates)
    {
        var existingCandidateEndResultIds = listEndResult.CandidateEndResults.Select(x => x.CandidateId).ToList();
        var newCandidateEndResults = candidates.Where(x => !existingCandidateEndResultIds.Contains(x.Id))
            .Select(x => new ProportionalElectionCandidateEndResult
            {
                CandidateId = x.Id,
                ListEndResult = listEndResult,
                Rank = DefaultRank,
                State = ProportionalElectionCandidateEndResultState.Pending,
            });
        foreach (var newCandidateEndResult in newCandidateEndResults)
        {
            listEndResult.CandidateEndResults.Add(newCandidateEndResult);
        }
    }

    internal void AdjustCandidateEndResults(
        ICollection<ProportionalElectionCandidateEndResult> candidateEndResults,
        IEnumerable<ProportionalElectionCandidateResult> candidateResults,
        int deltaFactor,
        bool lotDecisionEnabled)
    {
        candidateEndResults.MatchAndExec(
            x => x.CandidateId,
            candidateResults,
            x => x.CandidateId,
            (endResult, result) =>
                {
                    endResult.ForEachSubTotal(result, (endResultSubTotal, resultsSubTotal) =>
                    {
                        endResultSubTotal.UnmodifiedListVotesCount += resultsSubTotal.UnmodifiedListVotesCount * deltaFactor;
                        endResultSubTotal.ModifiedListVotesCount += resultsSubTotal.ModifiedListVotesCount * deltaFactor;
                        endResultSubTotal.CountOfVotesOnOtherLists += resultsSubTotal.CountOfVotesOnOtherLists * deltaFactor;
                        endResultSubTotal.CountOfVotesFromAccumulations += resultsSubTotal.CountOfVotesFromAccumulations * deltaFactor;
                    });

                    AdjustCandidateVoteSources(endResult, result, deltaFactor);
                });

        RecalculateCandidateEndResultRanks(candidateEndResults, lotDecisionEnabled);
    }

    internal void RecalculateCandidateEndResultStates(
        ProportionalElectionEndResult proportionalElectionEndResult)
    {
        if (!proportionalElectionEndResult.AllCountingCirclesDone)
        {
            SetCandidateEndResultStatesToPending(proportionalElectionEndResult.ListEndResults.SelectMany(x => x.CandidateEndResults));
            return;
        }

        foreach (var listEndResult in proportionalElectionEndResult.ListEndResults)
        {
            RecalculateCandidateEndResultStates(listEndResult);
        }
    }

    internal void RecalculateCandidateEndResultStates(
        ProportionalElectionListEndResult listEndResult)
    {
        SetCandidateEndResultStatesAfterAllSubmissionsDone(listEndResult);
    }

    internal void UpdateCandidateEndResultRanksByLotDecisions(
        ProportionalElectionListEndResult listEndResult,
        IEnumerable<ElectionEndResultLotDecision> lotDecisions)
    {
        var candidateEndResultsByCandidateId = listEndResult.CandidateEndResults.ToDictionary(x => x.CandidateId);
        foreach (var lotDecision in lotDecisions)
        {
            var candidateEndResult = candidateEndResultsByCandidateId[lotDecision.CandidateId];
            candidateEndResult.Rank = lotDecision.Rank;
            candidateEndResult.LotDecision = true;
        }
    }

    internal async Task SetCandidateEndResultsManually(Guid listId, Dictionary<Guid, ProportionalElectionCandidateEndResultState> candidateStateById)
    {
        var listEndResult = await _dataContext
            .ProportionalElectionListEndResult
            .AsTracking()
            .Include(l => l.ElectionEndResult)
            .Include(l => l.CandidateEndResults)
            .FirstOrDefaultAsync(l => l.ListId == listId)
            ?? throw new EntityNotFoundException(listId);

        var simpleEndResult = await _simplePoliticalBusinessRepo.Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == listEndResult.ElectionEndResult.ProportionalElectionId)
            ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), listEndResult.ElectionEndResult.ProportionalElectionId);

        foreach (var candidateEndResult in listEndResult.CandidateEndResults)
        {
            if (!candidateStateById.TryGetValue(candidateEndResult.CandidateId, out var candidateState))
            {
                throw new InvalidOperationException($"Cannot find proportional election manual state for candidate {candidateEndResult.CandidateId}");
            }

            candidateEndResult.State = candidateState;
        }

        listEndResult.ElectionEndResult.Finalized = false;
        simpleEndResult.EndResultFinalized = false;
        listEndResult.NumberOfMandates = listEndResult.CandidateEndResults.Count(x => x.State == ProportionalElectionCandidateEndResultState.Elected);
        await _dataContext.SaveChangesAsync();
    }

    internal void RecalculateLotDecisionRequired(ProportionalElectionListEndResult listEndResult)
    {
        var enabledCandidateEndResults = listEndResult.CandidateEndResults.Where(x => x.LotDecisionEnabled);

        listEndResult.HasOpenRequiredLotDecisions = false;
        foreach (var candidateEndResult in enabledCandidateEndResults)
        {
            // lot decision is always required when there are candidates with the same vote count
            candidateEndResult.LotDecisionRequired = true;
            listEndResult.HasOpenRequiredLotDecisions |= candidateEndResult.LotDecisionRequired && !candidateEndResult.LotDecision;
        }
    }

    private void AddMissingEndResultToCandidate(ProportionalElectionCandidate candidate)
    {
        candidate.EndResult ??= new ProportionalElectionCandidateEndResult
        {
            CandidateId = candidate.Id,
            ListEndResult = candidate.ProportionalElectionList.EndResult!,
            Rank = DefaultRank,
            State = ProportionalElectionCandidateEndResultState.Pending,
        };
    }

    private void SetCandidateEndResultStatesAfterAllSubmissionsDone(ProportionalElectionListEndResult listEndResult)
    {
        foreach (var candidateEndResult in listEndResult.CandidateEndResults)
        {
            SetCandidateEndResultStateAfterAllSubmissionsDone(candidateEndResult, listEndResult.NumberOfMandates);
        }
    }

    private void SetCandidateEndResultStateAfterAllSubmissionsDone(
        ProportionalElectionCandidateEndResult candidateEndResult,
        int numberOfMandates)
    {
        if (candidateEndResult.Rank > numberOfMandates)
        {
            candidateEndResult.State = ProportionalElectionCandidateEndResultState.NotElected;
            return;
        }

        candidateEndResult.State = candidateEndResult.LotDecisionEnabled && !candidateEndResult.LotDecision
            ? ProportionalElectionCandidateEndResultState.Pending
            : ProportionalElectionCandidateEndResultState.Elected;
    }

    private void SetCandidateEndResultStatesToPending(IEnumerable<ProportionalElectionCandidateEndResult> candidateEndResults)
    {
        foreach (var candidateEndResult in candidateEndResults)
        {
            candidateEndResult.State = ProportionalElectionCandidateEndResultState.Pending;
        }
    }

    private void AdjustCandidateVoteSources(
        ProportionalElectionCandidateEndResult candidateEndResult,
        ProportionalElectionCandidateResult candidateResult,
        int deltaFactor)
    {
        var candidateEndResultVoteSources = candidateEndResult.VoteSources.ToDictionary(x => x.ListId ?? Guid.Empty);
        foreach (var voteSource in candidateResult.VoteSources)
        {
            if (!candidateEndResultVoteSources.TryGetValue(voteSource.ListId ?? Guid.Empty, out var endResultVoteSource))
            {
                endResultVoteSource = new ProportionalElectionCandidateVoteSourceEndResult
                {
                    ListId = voteSource.ListId,
                    CandidateResult = candidateEndResult,
                    CandidateResultId = candidateEndResult.Id,
                };
                candidateEndResult.VoteSources.Add(endResultVoteSource);
            }

            endResultVoteSource.AdjustVoteCounts(voteSource, deltaFactor);
        }
    }
}
