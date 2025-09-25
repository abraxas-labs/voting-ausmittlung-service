// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public class MajorityElectionRelativeMajorityStrategy : MajorityElectionMandateAlgorithmStrategy
{
    public override MajorityElectionMandateAlgorithm MandateAlgorithm => MajorityElectionMandateAlgorithm.RelativeMajority;

    public override void RecalculatePrimaryCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult)
    {
        SetCandidateEndResultStatesToPending(majorityElectionEndResult);

        if (!majorityElectionEndResult.AllCountingCirclesDone)
        {
            ResetCalculation(majorityElectionEndResult);
            return;
        }

        SetCandidateEndResultStatesAfterAllSubmissionsDone(majorityElectionEndResult.CandidateEndResults, majorityElectionEndResult.MajorityElection.NumberOfMandates);

        if (majorityElectionEndResult.CandidateEndResults.Any(x => x.LotDecisionRequired && !x.LotDecision))
        {
            SetSecondaryCandidateEndResultStatesToPending(majorityElectionEndResult);
            return;
        }

        RecalculateSecondaryCandidateEndResultStates(majorityElectionEndResult);
    }

    public override void RecalculateSecondaryCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult)
    {
        SetSecondaryCandidateEndResultStatesToPending(majorityElectionEndResult);

        if (!majorityElectionEndResult.AllCountingCirclesDone)
        {
            ResetCalculation(majorityElectionEndResult);
            return;
        }

        SetSecondaryCandidateResultStatesDependentOfPrimaryElection(
            majorityElectionEndResult.CandidateEndResults,
            majorityElectionEndResult.SecondaryMajorityElectionEndResults.SelectMany(x => x.CandidateEndResults));

        foreach (var secondaryMajorityElectionEndResult in majorityElectionEndResult.SecondaryMajorityElectionEndResults)
        {
            SetCandidateEndResultStatesAfterAllSubmissionsDone(secondaryMajorityElectionEndResult.CandidateEndResults, secondaryMajorityElectionEndResult.SecondaryMajorityElection.NumberOfMandates);
        }
    }

    public override void SetCandidateElected<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase> context)
    {
        candidateEndResult.State = MajorityElectionCandidateEndResultState.Elected;
        context.IncreaseDistributedNumberOfMandates();
    }

    public override void SetCandidateNotElected<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase> context)
    {
        candidateEndResult.State = MajorityElectionCandidateEndResultState.NotElected;
    }

    private void SetCandidateEndResultStatesAfterAllSubmissionsDone<TMajorityElectionCandidateEndResultBase>(
        IEnumerable<TMajorityElectionCandidateEndResultBase> candidateEndResults,
        int numberOfMandates)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
    {
        var sortedCandidateEndResults = candidateEndResults
            .OrderByDescending(x => x.VoteCount)
            .ThenBy(x => x.Rank)
            .ToList();

        var context = new MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase>(numberOfMandates, candidateEndResults);

        foreach (var candidateEndResult in sortedCandidateEndResults)
        {
            SetCandidateEndResultStateAfterAllSubmissionsDone(candidateEndResult, context);
        }

        RecalculateRankIfLotDecisionPending(candidateEndResults);
    }
}
