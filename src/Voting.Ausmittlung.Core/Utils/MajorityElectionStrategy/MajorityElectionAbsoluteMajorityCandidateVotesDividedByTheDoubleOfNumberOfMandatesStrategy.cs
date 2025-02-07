// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public class MajorityElectionAbsoluteMajorityCandidateVotesDividedByTheDoubleOfNumberOfMandatesStrategy : MajorityElectionAbsoluteMajorityStrategy
{
    public override CantonMajorityElectionAbsoluteMajorityAlgorithm? AbsoluteMajorityAlgorithm => CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates;

    public override void CalculateAbsoluteMajority(MajorityElectionEndResult majorityElectionEndResult)
    {
        CalculateAbsoluteMajority(
            majorityElectionEndResult.Calculation,
            majorityElectionEndResult.MajorityElection.NumberOfMandates,
            majorityElectionEndResult);
    }

    protected override void CalculateAbsoluteMajority(
        MajorityElectionEndResult primaryEndResult,
        SecondaryMajorityElectionEndResult secondaryEndResult)
    {
        CalculateAbsoluteMajority(
            secondaryEndResult.Calculation,
            secondaryEndResult.SecondaryMajorityElection.NumberOfMandates,
            secondaryEndResult);
    }

    private void CalculateAbsoluteMajority(
        MajorityElectionEndResultCalculation calculation,
        int numberOfMandates,
        IMajorityElectionResultTotal<int> resultTotal)
    {
        if (numberOfMandates == 0)
        {
            throw new ArgumentException($"{nameof(MajorityElection.NumberOfMandates)} must not be 0");
        }

        calculation.DecisiveVoteCount = resultTotal.TotalCandidateVoteCountInclIndividual;
        calculation.AbsoluteMajorityThreshold = (decimal)calculation.DecisiveVoteCount / numberOfMandates / 2;
        calculation.AbsoluteMajority = (int)Math.Floor(calculation.AbsoluteMajorityThreshold.Value) + 1;
    }
}
