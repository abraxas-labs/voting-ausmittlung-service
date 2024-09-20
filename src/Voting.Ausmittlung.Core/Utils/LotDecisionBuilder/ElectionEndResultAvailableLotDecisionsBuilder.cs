// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.LotDecisionBuilder;

public abstract class ElectionEndResultAvailableLotDecisionsBuilder
{
    protected List<TElectionEndResultAvailableLotDecision> BuildAvailableLotDecisions<
        TElectionEndResultAvailableLotDecision,
        TElectionCandidateEndResult,
        TElectionCandidate>(
        IEnumerable<TElectionCandidateEndResult> candidateEndResults,
        Func<TElectionCandidateEndResult, TElectionCandidate> candidateFunc)
        where TElectionEndResultAvailableLotDecision : ElectionEndResultAvailableLotDecision<TElectionCandidate>, new()
        where TElectionCandidateEndResult : ElectionCandidateEndResult
        where TElectionCandidate : ElectionCandidate
    {
        var enabledCandidateEndResults = candidateEndResults.Where(x => x.LotDecisionEnabled).ToList();

        var candidateEndResultsMinAndCountByVoteCount = enabledCandidateEndResults
            .GroupBy(candEndResult => candEndResult.VoteCount)
            .ToDictionary(x => x.Key, x => new
            {
                MinRank = x.Min(candEndResult => candEndResult.Rank),
                Count = x.Count(),
            });

        return enabledCandidateEndResults
            .Select(x => new TElectionEndResultAvailableLotDecision
            {
                Candidate = candidateFunc(x),
                VoteCount = x.VoteCount,
                LotDecisionRequired = x.LotDecisionRequired,
                SelectableRanks = BuildSelectableRanks(
                    candidateEndResultsMinAndCountByVoteCount[x.VoteCount].MinRank,
                    candidateEndResultsMinAndCountByVoteCount[x.VoteCount].Count),
                OriginalRank = x.Rank,
                SelectedRank = x.LotDecision ? x.Rank : (int?)null,
            })
            .OrderBy(x => x.OriginalRank)
            .ThenBy(x => x.Candidate.Position)
            .ToList();
    }

    private List<int> BuildSelectableRanks(int minRank, int count)
    {
        return Enumerable.Range(minRank, count).ToList();
    }
}
