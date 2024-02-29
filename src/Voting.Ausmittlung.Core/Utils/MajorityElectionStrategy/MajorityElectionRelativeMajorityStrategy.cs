// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public class MajorityElectionRelativeMajorityStrategy : MajorityElectionMandateAlgorithmStrategy
{
    public override MajorityElectionMandateAlgorithm MandateAlgorithm => MajorityElectionMandateAlgorithm.RelativeMajority;

    public override void RecalculateCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult)
    {
        if (!majorityElectionEndResult.AllCountingCirclesDone)
        {
            SetCandidateEndResultStatesToPending(majorityElectionEndResult);
            return;
        }

        SetCandidateEndResultStatesAfterAllSubmissionsDone(majorityElectionEndResult.CandidateEndResults, majorityElectionEndResult.MajorityElection.NumberOfMandates);

        foreach (var secondaryMajorityElectionEndResult in majorityElectionEndResult.SecondaryMajorityElectionEndResults)
        {
            SetCandidateEndResultStatesAfterAllSubmissionsDone(secondaryMajorityElectionEndResult.CandidateEndResults, secondaryMajorityElectionEndResult.SecondaryMajorityElection.NumberOfMandates);
        }
    }

    private void SetCandidateEndResultStatesAfterAllSubmissionsDone<TMajorityElectionCandidateEndResultBase>(
        IEnumerable<TMajorityElectionCandidateEndResultBase> candidateEndResults,
        int numberOfMandates)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
    {
        foreach (var candidateEndResult in candidateEndResults)
        {
            SetCandidateEndResultStateAfterAllSubmissionsDone(candidateEndResult, numberOfMandates);
        }

        RecalculateLotDecisionRequired(candidateEndResults, numberOfMandates);
    }

    private void SetCandidateEndResultStateAfterAllSubmissionsDone<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        int numberOfMandates)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
    {
        candidateEndResult.State = candidateEndResult.Rank > numberOfMandates
            ? MajorityElectionCandidateEndResultState.NotElected
            : MajorityElectionCandidateEndResultState.Elected;
    }
}
