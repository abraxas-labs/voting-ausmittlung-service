// (c) Copyright by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Core.Utils;

public class MajorityElectionCandidateEndResultBuilder : ElectionCandidateEndResultBuilder
{
    private const int DefaultRank = 1;

    private readonly MajorityElectionCandidateRepo _majorityElectionCandidateRepo;
    private readonly SecondaryMajorityElectionCandidateRepo _secondaryMajorityElectionCandidateRepo;
    private readonly DataContext _dataContext;

    public MajorityElectionCandidateEndResultBuilder(
        MajorityElectionCandidateRepo majorityElectionCandidateRepo,
        SecondaryMajorityElectionCandidateRepo secondaryMajorityElectionCandidateRepo,
        DataContext dataContext)
    {
        _majorityElectionCandidateRepo = majorityElectionCandidateRepo;
        _secondaryMajorityElectionCandidateRepo = secondaryMajorityElectionCandidateRepo;
        _dataContext = dataContext;
    }

    internal async Task Initialize(Guid majorityElectionCandidateId)
    {
        var candidate = await _majorityElectionCandidateRepo.GetWithEndResultsAsTracked(majorityElectionCandidateId)
            ?? throw new EntityNotFoundException(majorityElectionCandidateId);

        if (candidate.EndResult != null)
        {
            return;
        }

        // Since the end result may already been calculated, we need to set the rank of this candidate
        var candidateEndResultWithLowestVotes = await _dataContext.MajorityElectionCandidateEndResults
            .Where(r => r.MajorityElectionEndResultId == candidate.MajorityElection.EndResult!.Id)
            .OrderBy(r => r.VoteCount)
            .FirstOrDefaultAsync();

        var rank = DefaultRank;
        if (candidateEndResultWithLowestVotes != null)
        {
            // vote count cannot be negative, checked by business rules
            rank = candidateEndResultWithLowestVotes.VoteCount == 0
                ? candidateEndResultWithLowestVotes.Rank
                : candidateEndResultWithLowestVotes.Rank + 1;
        }

        candidate.EndResult = new MajorityElectionCandidateEndResult
        {
            CandidateId = candidate.Id,
            MajorityElectionEndResult = candidate.MajorityElection.EndResult!,
            Rank = rank,
            State = MajorityElectionCandidateEndResultState.Pending,
        };
        await _dataContext.SaveChangesAsync();
    }

    internal async Task InitializeForSecondaryMajorityElectionCandidate(Guid secondaryMajorityElectionCandidateId)
    {
        var candidate = await _secondaryMajorityElectionCandidateRepo.GetWithEndResultsAsTracked(secondaryMajorityElectionCandidateId)
            ?? throw new EntityNotFoundException(secondaryMajorityElectionCandidateId);

        if (candidate.EndResult != null)
        {
            return;
        }

        // Since the end result may already been calculated, we need to set the rank of this candidate
        var candidateEndResultWithLowestVotes = await _dataContext.SecondaryMajorityElectionCandidateEndResults
            .Where(r => r.SecondaryMajorityElectionEndResultId == candidate.SecondaryMajorityElection!.EndResult!.Id)
            .OrderBy(r => r.VoteCount)
            .FirstOrDefaultAsync();

        var rank = DefaultRank;
        if (candidateEndResultWithLowestVotes != null)
        {
            // vote count cannot be negative, checked by business rules
            rank = candidateEndResultWithLowestVotes.VoteCount == 0
                ? candidateEndResultWithLowestVotes.Rank
                : candidateEndResultWithLowestVotes.Rank + 1;
        }

        candidate.EndResult = new SecondaryMajorityElectionCandidateEndResult
        {
            CandidateId = candidate.Id,
            SecondaryMajorityElectionEndResult = candidate.SecondaryMajorityElection.EndResult!,
            Rank = rank,
            State = MajorityElectionCandidateEndResultState.Pending,
        };
        await _dataContext.SaveChangesAsync();
    }

    internal void AddMissingMajorityElectionCandidateEndResults(
        MajorityElectionEndResult majorityElectionEndResult,
        IEnumerable<MajorityElectionCandidate> majorityElectionCandidates)
    {
        var existingCandidateEndResultIds = majorityElectionEndResult.CandidateEndResults.Select(x => x.CandidateId).ToList();
        var newCandidateEndResults = majorityElectionCandidates.Where(x => !existingCandidateEndResultIds.Contains(x.Id))
            .Select(x => new MajorityElectionCandidateEndResult
            {
                CandidateId = x.Id,
                MajorityElectionEndResult = majorityElectionEndResult,
                Rank = DefaultRank,
                State = MajorityElectionCandidateEndResultState.Pending,
            });
        foreach (var newCandidateEndResult in newCandidateEndResults)
        {
            majorityElectionEndResult.CandidateEndResults.Add(newCandidateEndResult);
        }
    }

    internal void AddMissingSecondaryMajorityElectionCandidateEndResults(
        SecondaryMajorityElectionEndResult secondaryMajorityElectionEndResult,
        IEnumerable<SecondaryMajorityElectionCandidate> secondaryMajorityElectionCandidates)
    {
        var existingCandidateEndResultIds = secondaryMajorityElectionEndResult.CandidateEndResults.Select(x => x.CandidateId).ToList();
        var newCandidateEndResults = secondaryMajorityElectionCandidates.Where(x => !existingCandidateEndResultIds.Contains(x.Id))
            .Select(x => new SecondaryMajorityElectionCandidateEndResult
            {
                CandidateId = x.Id,
                SecondaryMajorityElectionEndResult = secondaryMajorityElectionEndResult,
                Rank = DefaultRank,
                State = MajorityElectionCandidateEndResultState.Pending,
            });
        foreach (var newCandidateEndResult in newCandidateEndResults)
        {
            secondaryMajorityElectionEndResult.CandidateEndResults.Add(newCandidateEndResult);
        }
    }

    internal void AdjustCandidateEndResults<TMajorityElectionCandidateEndResultBase, TMajorityElectionCandidateResultBase>(
        ICollection<TMajorityElectionCandidateEndResultBase> endResults,
        IEnumerable<TMajorityElectionCandidateResultBase> results,
        int deltaFactor,
        bool allCountingCirclesDone)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
        where TMajorityElectionCandidateResultBase : MajorityElectionCandidateResultBase
    {
        endResults.MatchAndExec(
            x => x.CandidateId,
            results,
            x => x.CandidateId,
            (endResult, result) => endResult.AdjustVoteCounts(result, deltaFactor));
        RecalculateCandidateEndResultRanks(endResults, allCountingCirclesDone);
    }

    internal void UpdateCandidateEndResultRanksByLotDecisions(
        MajorityElectionEndResult endResult,
        IEnumerable<ElectionEndResultLotDecision> lotDecisions)
    {
        var candidateEndResultsByCandidateId = endResult.PrimaryAndSecondaryCandidateEndResults
            .ToDictionary(x => x.CandidateId, x => x);

        foreach (var lotDecision in lotDecisions)
        {
            var candidateEndResult = candidateEndResultsByCandidateId[lotDecision.CandidateId];

            if (lotDecision.Rank.HasValue)
            {
                candidateEndResult.Rank = lotDecision.Rank.Value;
                candidateEndResult.LotDecision = true;
            }
            else
            {
                candidateEndResult.LotDecision = false;
            }
        }
    }
}
