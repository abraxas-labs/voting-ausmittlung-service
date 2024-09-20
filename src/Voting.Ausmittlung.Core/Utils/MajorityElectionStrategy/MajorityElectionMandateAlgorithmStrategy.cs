// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public abstract class MajorityElectionMandateAlgorithmStrategy : IMajorityElectionMandateAlgorithmStrategy
{
    public abstract MajorityElectionMandateAlgorithm MandateAlgorithm { get; }

    public virtual CantonMajorityElectionAbsoluteMajorityAlgorithm? AbsoluteMajorityAlgorithm => null;

    public abstract void RecalculateCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult);

    protected void SetCandidateEndResultStatesToPending(MajorityElectionEndResult majorityElectionEndResult)
    {
        foreach (var candidateEndResult in majorityElectionEndResult.PrimaryAndSecondaryCandidateEndResults)
        {
            candidateEndResult.State = MajorityElectionCandidateEndResultState.Pending;
        }
    }

    protected void RecalculateLotDecisionRequired<TMajorityElectionCandidateEndResultBase>(
        IEnumerable<TMajorityElectionCandidateEndResultBase> candidateEndResults,
        int numberOfMandates)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
    {
        var enabledCandidateEndResults = candidateEndResults.Where(x => x.LotDecisionEnabled);

        var candidateEndResultMinMaxRankByVoteCount = enabledCandidateEndResults
            .GroupBy(candEndResult => candEndResult.VoteCount)
            .ToDictionary(x => x.Key, x => new
            {
                MinRank = x.Min(candEndResult => candEndResult.Rank),
                MaxRank = x.Min(candEndResult => candEndResult.Rank) + x.Count() - 1,
            });

        foreach (var candidateEndResult in enabledCandidateEndResults)
        {
            var candidateEndResultMinMaxRank = candidateEndResultMinMaxRankByVoteCount[candidateEndResult.VoteCount];

            // lot decision is required when there are candidates with the same vote count
            // and the elected candidates is dependent of this candidate rank
            candidateEndResult.LotDecisionRequired =
                candidateEndResultMinMaxRank.MinRank <= numberOfMandates
                && candidateEndResultMinMaxRank.MaxRank > numberOfMandates
                && candidateEndResult.State != MajorityElectionCandidateEndResultState.NoAbsoluteMajorityAndNotElectedButRankOk;

            if (candidateEndResult.LotDecisionRequired && !candidateEndResult.LotDecision)
            {
                candidateEndResult.State = MajorityElectionCandidateEndResultState.Pending;
            }
        }
    }
}
