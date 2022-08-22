﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public class MajorityElectionAbsoluteMajorityStrategy : MajorityElectionMandateAlgorithmStrategy
{
    // no override of AbsoluteMajorityAlgorithm, since ValidBallotsDividedByTwo (which is implemented here), should be the default.
    public override MajorityElectionMandateAlgorithm MandateAlgorithm => MajorityElectionMandateAlgorithm.AbsoluteMajority;

    public override void RecalculateCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult)
    {
        if (!majorityElectionEndResult.AllCountingCirclesDone)
        {
            SetCandidateEndResultStatesToPending(majorityElectionEndResult);
            return;
        }

        var absoluteMajority = CalculateAbsoluteMajority(majorityElectionEndResult);
        majorityElectionEndResult.Calculation.AbsoluteMajority = absoluteMajority;

        SetCandidateEndResultStatesAfterAllSubmissionsDone(
            majorityElectionEndResult.CandidateEndResults,
            majorityElectionEndResult.MajorityElection.NumberOfMandates,
            absoluteMajority);

        foreach (var secondaryMajorityElectionEndResult in majorityElectionEndResult.SecondaryMajorityElectionEndResults)
        {
            SetCandidateEndResultStatesAfterAllSubmissionsDone(
                secondaryMajorityElectionEndResult.CandidateEndResults,
                secondaryMajorityElectionEndResult.SecondaryMajorityElection.NumberOfMandates,
                absoluteMajority);
        }
    }

    public virtual int CalculateAbsoluteMajority(MajorityElectionEndResult majorityElectionEndResult)
    {
        var decisiveSimpleVoteCount = majorityElectionEndResult.CountOfVoters.TotalAccountedBallots;
        return (decisiveSimpleVoteCount / 2) + 1;
    }

    private void SetCandidateEndResultStatesAfterAllSubmissionsDone<TMajorityElectionCandidateEndResultBase>(
        IEnumerable<TMajorityElectionCandidateEndResultBase> candidateEndResults,
        int numberOfMandates,
        int absoluteMajority)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
    {
        foreach (var candidateEndResult in candidateEndResults)
        {
            SetCandidateEndResultStateAfterAllSubmissionsDone(candidateEndResult, numberOfMandates, absoluteMajority);
        }

        RecalculateLotDecisionRequired(candidateEndResults, numberOfMandates);
    }

    private void SetCandidateEndResultStateAfterAllSubmissionsDone<TMajorityElectionCandidateEndResultBase>(
        TMajorityElectionCandidateEndResultBase candidateEndResult,
        int numberOfMandates,
        int absoluteMajority)
        where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
    {
        var hasAbsoluteMajority = candidateEndResult.VoteCount >= absoluteMajority;

        if (candidateEndResult.Rank > numberOfMandates)
        {
            candidateEndResult.State = hasAbsoluteMajority
                ? MajorityElectionCandidateEndResultState.AbsoluteMajorityAndNotElected
                : MajorityElectionCandidateEndResultState.NotElected;
            return;
        }

        if (!hasAbsoluteMajority)
        {
            candidateEndResult.State = MajorityElectionCandidateEndResultState.NoAbsoluteMajorityAndNotElectedButRankOk;
            return;
        }

        candidateEndResult.State = candidateEndResult.LotDecisionEnabled && !candidateEndResult.LotDecision
            ? MajorityElectionCandidateEndResultState.Pending
            : MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected;
    }
}
