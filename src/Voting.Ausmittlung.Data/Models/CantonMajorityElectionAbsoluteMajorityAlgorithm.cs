// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum CantonMajorityElectionAbsoluteMajorityAlgorithm
{
    /// <summary>
    /// Majority election absolute majority algorithm is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Calculate mandates using absolute majority calculation where the count of ballots are divided by 2.
    /// </summary>
    ValidBallotsDividedByTwo,

    /// <summary>
    /// Calculate mandates using absolute majority calculation where candidate votes are divided by the double of number of mandates.
    /// </summary>
    CandidateVotesDividedByTheDoubleOfNumberOfMandates,
}
