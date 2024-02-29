// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum SecondaryMajorityElectionAllowedCandidate
{
    /// <summary>
    /// Secondary majority election allowed candidate is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The candidates of the secondary majority elections must exist in the primary election.
    /// </summary>
    MustExistInPrimaryElection,

    /// <summary>
    /// The candidates of the secondary majority elections may exist in the primary election.
    /// </summary>
    MayExistInPrimaryElection,

    /// <summary>
    /// The candidates of the secondary majority elections must not exist in the primary election.
    /// </summary>
    MustNotExistInPrimaryElection,
}
