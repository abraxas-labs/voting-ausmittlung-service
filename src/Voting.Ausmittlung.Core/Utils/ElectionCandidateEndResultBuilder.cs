// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils;

public abstract class ElectionCandidateEndResultBuilder
{
    protected void RecalculateCandidateEndResultRanks<TElectionCandidateEndResultBase>(
        IEnumerable<TElectionCandidateEndResultBase> candidateEndResults,
        bool allCountingCirclesDone)
        where TElectionCandidateEndResultBase : ElectionCandidateEndResult
    {
        var orderedCandidateEndResults = candidateEndResults
            .OrderByDescending(x => x.VoteCount)
            .ThenBy(x => x.Rank)
            .ThenBy(x => x.CandidateId)
            .ToList();

        var voteCountsWithMultipleCandidateEndResults = candidateEndResults
            .GroupBy(x => x.VoteCount)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        var rankBefore = 1;
        var countBefore = int.MaxValue;

        for (var pos = 1; pos <= orderedCandidateEndResults.Count; pos++)
        {
            var candidateEndResult = orderedCandidateEndResults[pos - 1];

            // this method is invoked when a result is added or removed, so there must not be a lot decision yet
            candidateEndResult.LotDecision = false;

            // lot decision required is calculated on adjust end result or update lot decisions
            candidateEndResult.LotDecisionRequired = false;

            if (!voteCountsWithMultipleCandidateEndResults.Contains(candidateEndResult.VoteCount))
            {
                candidateEndResult.Rank = pos;
                candidateEndResult.LotDecisionEnabled = false;
                rankBefore = pos;
                countBefore = candidateEndResult.VoteCount;
                continue;
            }

            candidateEndResult.LotDecisionEnabled = allCountingCirclesDone;

            if (candidateEndResult.VoteCount == countBefore)
            {
                candidateEndResult.Rank = rankBefore;
                continue;
            }

            candidateEndResult.Rank = pos;
            rankBefore = pos;
            countBefore = candidateEndResult.VoteCount;
        }
    }
}
