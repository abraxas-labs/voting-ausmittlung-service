// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum MajorityElectionMandateAlgorithm
{
    /// <summary>
    /// Majority election mandate algorithm is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Calculate mandates using absolute majority.
    /// </summary>
    AbsoluteMajority,

    /// <summary>
    /// Calculate mandates using relative majority.
    /// </summary>
    RelativeMajority,
}
