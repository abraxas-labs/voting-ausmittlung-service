// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum MajorityElectionResultEntry
{
    /// <summary>
    /// Majority election result entry is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Enter only the final results of the election.
    /// </summary>
    FinalResults,

    /// <summary>
    /// Enter detailed election results.
    /// </summary>
    Detailed,
}
