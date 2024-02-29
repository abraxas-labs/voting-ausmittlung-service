// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum PoliticalBusinessType
{
    /// <summary>
    /// Political business type is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// A vote.
    /// </summary>
    Vote,

    /// <summary>
    /// A majority election.
    /// </summary>
    MajorityElection,

    /// <summary>
    /// A proportional election.
    /// </summary>
    ProportionalElection,

    /// <summary>
    /// A secondary majority election.
    /// </summary>
    SecondaryMajorityElection,
}
