// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public class MajorityElectionAbsoluteMajorityCandidateVotesDividedByTheDoubleOfNumberOfMandatesStrategy : MajorityElectionAbsoluteMajorityStrategy
{
    public override CantonMajorityElectionAbsoluteMajorityAlgorithm? AbsoluteMajorityAlgorithm => CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates;

    public override int CalculateAbsoluteMajority(MajorityElectionEndResult majorityElectionEndResult)
    {
        var nrOfMandates = majorityElectionEndResult.MajorityElection.NumberOfMandates;
        if (nrOfMandates == 0)
        {
            throw new ArgumentException($"{nameof(MajorityElection.NumberOfMandates)} must not be 0");
        }

        var decisiveVoteCount = majorityElectionEndResult.TotalCandidateVoteCountInclIndividual;
        return (decisiveVoteCount / nrOfMandates / 2) + 1;
    }
}
