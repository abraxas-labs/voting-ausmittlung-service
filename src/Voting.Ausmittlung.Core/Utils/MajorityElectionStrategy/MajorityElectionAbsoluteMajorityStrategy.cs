// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public class MajorityElectionAbsoluteMajorityStrategy : MajorityElectionMandateAlgorithmStrategy
{
    // no override of AbsoluteMajorityAlgorithm, since ValidBallotsDividedByTwo (which is implemented here), should be the default.
    public override MajorityElectionMandateAlgorithm MandateAlgorithm => MajorityElectionMandateAlgorithm.AbsoluteMajority;

    public override void RecalculatePrimaryCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult)
    {
        SetCandidateEndResultStatesToPending(majorityElectionEndResult);

        if (!majorityElectionEndResult.AllCountingCirclesDone)
        {
            return;
        }

        CalculateAbsoluteMajority(majorityElectionEndResult);
        foreach (var secondaryEndResult in majorityElectionEndResult.SecondaryMajorityElectionEndResults)
        {
            CalculateAbsoluteMajority(majorityElectionEndResult, secondaryEndResult);
        }

        SetCandidateEndResultStatesAfterAllSubmissionsDone(
            majorityElectionEndResult.CandidateEndResults,
            majorityElectionEndResult.MajorityElection.NumberOfMandates,
            majorityElectionEndResult.Calculation.AbsoluteMajority!.Value);

        if (majorityElectionEndResult.CandidateEndResults.Any(x => x.LotDecisionRequired && !x.LotDecision))
        {
            return;
        }

        RecalculateSecondaryCandidateEndResultStates(majorityElectionEndResult);
    }

    public override void RecalculateSecondaryCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult)
    {
        SetSecondaryCandidateEndResultStatesToPending(majorityElectionEndResult);

        if (!majorityElectionEndResult.AllCountingCirclesDone)
        {
            return;
        }

        SetSecondaryCandidateResultStatesDependentOfPrimaryElection(
            majorityElectionEndResult.CandidateEndResults,
            majorityElectionEndResult.SecondaryMajorityElectionEndResults.SelectMany(x => x.CandidateEndResults));

        foreach (var secondaryMajorityElectionEndResult in majorityElectionEndResult.SecondaryMajorityElectionEndResults)
        {
            SetCandidateEndResultStatesAfterAllSubmissionsDone(
                secondaryMajorityElectionEndResult.CandidateEndResults,
                secondaryMajorityElectionEndResult.SecondaryMajorityElection.NumberOfMandates,
                secondaryMajorityElectionEndResult.Calculation.AbsoluteMajority!.Value);
        }
    }

    public virtual void CalculateAbsoluteMajority(MajorityElectionEndResult majorityElectionEndResult)
    {
        majorityElectionEndResult.Calculation.DecisiveVoteCount = majorityElectionEndResult.CountOfVoters.TotalAccountedBallots;
        majorityElectionEndResult.Calculation.AbsoluteMajorityThreshold = majorityElectionEndResult.Calculation.DecisiveVoteCount / 2M;
        majorityElectionEndResult.Calculation.AbsoluteMajority = (int)Math.Floor(majorityElectionEndResult.Calculation.AbsoluteMajorityThreshold.Value) + 1;
    }

    public override void SetCandidateElected<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase> context)
    {
        var hasAbsoluteMajority = candidateEndResult.VoteCount >= context.AbsoluteMajority;

        candidateEndResult.State = hasAbsoluteMajority
            ? MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected
            : MajorityElectionCandidateEndResultState.NoAbsoluteMajorityAndNotElectedButRankOk;
        context.IncreaseDistributedNumberOfMandates();
    }

    public override void SetCandidateNotElected<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase> context)
    {
        if (candidateEndResult.State == MajorityElectionCandidateEndResultState.AbsoluteMajorityAndNotElectedInPrimaryElectionNotEligible)
        {
            return;
        }

        var hasAbsoluteMajority = candidateEndResult.VoteCount >= context.AbsoluteMajority;

        candidateEndResult.State = hasAbsoluteMajority
            ? MajorityElectionCandidateEndResultState.AbsoluteMajorityAndNotElected
            : MajorityElectionCandidateEndResultState.NotElected;
    }

    protected virtual void CalculateAbsoluteMajority(MajorityElectionEndResult primaryEndResult, SecondaryMajorityElectionEndResult secondaryEndResult)
    {
        secondaryEndResult.Calculation.DecisiveVoteCount = primaryEndResult.Calculation.DecisiveVoteCount;
        secondaryEndResult.Calculation.AbsoluteMajorityThreshold = primaryEndResult.Calculation.AbsoluteMajorityThreshold;
        secondaryEndResult.Calculation.AbsoluteMajority = primaryEndResult.Calculation.AbsoluteMajority;
    }

    private void SetCandidateEndResultStatesAfterAllSubmissionsDone<TMajorityElectionCandidateEndResultBase>(
        IEnumerable<TMajorityElectionCandidateEndResultBase> candidateEndResults,
        int numberOfMandates,
        int absoluteMajority)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
    {
        var sortedCandidateEndResults = candidateEndResults
            .OrderByDescending(x => x.VoteCount)
            .ThenBy(x => x.Rank)
            .ToList();

        var context = new MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase>(numberOfMandates, candidateEndResults, absoluteMajority);

        foreach (var candidateEndResult in sortedCandidateEndResults)
        {
            SetCandidateEndResultStateAfterAllSubmissionsDone(candidateEndResult, context);
        }

        RecalculateRankIfLotDecisionPending(candidateEndResults);
    }
}
