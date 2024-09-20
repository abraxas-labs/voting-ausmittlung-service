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
        var nrOfMandates = majorityElectionEndResult.MajorityElection.NumberOfMandates;
        if (nrOfMandates == 0)
        {
            throw new ArgumentException($"{nameof(MajorityElection.NumberOfMandates)} must not be 0");
        }

        majorityElectionEndResult.Calculation.DecisiveVoteCount = majorityElectionEndResult.TotalCandidateVoteCountInclIndividual;
        majorityElectionEndResult.Calculation.AbsoluteMajorityThreshold = (decimal)majorityElectionEndResult.Calculation.DecisiveVoteCount / nrOfMandates / 2;
        majorityElectionEndResult.Calculation.AbsoluteMajority = (int)Math.Floor(majorityElectionEndResult.Calculation.AbsoluteMajorityThreshold.Value) + 1;
    }
}
