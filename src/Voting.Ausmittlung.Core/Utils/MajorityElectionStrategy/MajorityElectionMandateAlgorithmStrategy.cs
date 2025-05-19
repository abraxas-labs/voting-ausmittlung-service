// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public abstract class MajorityElectionMandateAlgorithmStrategy : IMajorityElectionMandateAlgorithmStrategy
{
    public abstract MajorityElectionMandateAlgorithm MandateAlgorithm { get; }

    public virtual CantonMajorityElectionAbsoluteMajorityAlgorithm? AbsoluteMajorityAlgorithm => null;

    public abstract void RecalculatePrimaryCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult);

    public abstract void RecalculateSecondaryCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult);

    public abstract void SetCandidateElected<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase> context)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase;

    public abstract void SetCandidateNotElected<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase> context)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase;

    protected void SetCandidateEndResultStateAfterAllSubmissionsDone<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase> context)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
    {
        var hasAbsoluteMajority = candidateEndResult.VoteCount >= context.AbsoluteMajority;

        if (context.AllNumberOfMandatesDistributed)
        {
            if (context.VoteCountWithRestMandatesInUncompletedLotDecision.HasValue &&
                context.VoteCountWithRestMandatesInUncompletedLotDecision == candidateEndResult.VoteCount &&
                candidateEndResult.State.IsEligible())
            {
                candidateEndResult.State = MajorityElectionCandidateEndResultState.Pending;
                candidateEndResult.LotDecisionRequired = true;
                return;
            }

            SetCandidateNotElected(candidateEndResult, context);
            return;
        }

        if (!candidateEndResult.State.IsEligible())
        {
            return;
        }

        var eligibleAndPendingCandidateEndResultsVoteCountGroup = context.SortedCandidateEndResultsByVoteCount[candidateEndResult.VoteCount]
            .Where(x => x.State == MajorityElectionCandidateEndResultState.Pending && x.State.IsEligible())
            .ToList();

        if (eligibleAndPendingCandidateEndResultsVoteCountGroup.Count == 1
            || context.RestMandates >= eligibleAndPendingCandidateEndResultsVoteCountGroup.Count
            || context.RestMandates >= eligibleAndPendingCandidateEndResultsVoteCountGroup.Count(x => x.Rank == candidateEndResult.Rank))
        {
            SetCandidateElected(candidateEndResult, context);
            return;
        }

        // If not all mandates can be distributed to all candidates in the same lot decision vote count group.
        candidateEndResult.State = MajorityElectionCandidateEndResultState.Pending;
        candidateEndResult.LotDecisionRequired = true;
        while (!context.AllNumberOfMandatesDistributed)
        {
            context.IncreaseDistributedNumberOfMandates();
        }

        context.VoteCountWithRestMandatesInUncompletedLotDecision = candidateEndResult.VoteCount;
    }

    protected void SetCandidateEndResultStatesToPending(MajorityElectionEndResult majorityElectionEndResult)
    {
        foreach (var candidateEndResult in majorityElectionEndResult.PrimaryAndSecondaryCandidateEndResults)
        {
            candidateEndResult.State = MajorityElectionCandidateEndResultState.Pending;
        }
    }

    protected void SetSecondaryCandidateEndResultStatesToPending(MajorityElectionEndResult majorityElectionEndResult)
    {
        foreach (var candidateEndResult in majorityElectionEndResult.SecondaryMajorityElectionEndResults.SelectMany(x => x.CandidateEndResults))
        {
            candidateEndResult.State = MajorityElectionCandidateEndResultState.Pending;
        }
    }

    protected void RecalculateRankIfLotDecisionPending<TMajorityElectionCandidateEndResultBase>(
        IEnumerable<TMajorityElectionCandidateEndResultBase> candidateEndResults)
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

            if (!candidateEndResult.LotDecision)
            {
                candidateEndResult.Rank = candidateEndResultMinMaxRank.MinRank;
            }
        }
    }

    protected void SetSecondaryCandidateResultStatesDependentOfPrimaryElection(
        IEnumerable<MajorityElectionCandidateEndResult> candidateEndResults,
        IEnumerable<SecondaryMajorityElectionCandidateEndResult> secondaryCandidateEndResults)
    {
        var primaryCandidateEndResultStateByRefId = candidateEndResults.ToDictionary(x => x.CandidateId, x => x.State);

        foreach (var secondaryCandidateEndResult in secondaryCandidateEndResults)
        {
            var refId = secondaryCandidateEndResult.Candidate.CandidateReferenceId;

            if (!refId.HasValue)
            {
                continue;
            }

            if (!primaryCandidateEndResultStateByRefId.TryGetValue(refId.Value, out var primaryCandidateEndResultState))
            {
                throw new InvalidOperationException("Secondary majority election candidate with a reference id which does not exist in the primary election found");
            }

            secondaryCandidateEndResult.State = primaryCandidateEndResultState switch
            {
                MajorityElectionCandidateEndResultState.AbsoluteMajorityAndNotElected => MajorityElectionCandidateEndResultState.AbsoluteMajorityAndNotElectedInPrimaryElectionNotEligible,
                MajorityElectionCandidateEndResultState.NoAbsoluteMajorityAndNotElectedButRankOk => MajorityElectionCandidateEndResultState.NotElectedInPrimaryElectionNotEligible,
                MajorityElectionCandidateEndResultState.NotElected => MajorityElectionCandidateEndResultState.NotElectedInPrimaryElectionNotEligible,
                _ => secondaryCandidateEndResult.State,
            };
        }
    }
}
