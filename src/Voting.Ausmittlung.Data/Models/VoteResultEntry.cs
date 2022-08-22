// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum VoteResultEntry
{
    /// <summary>
    /// Vote result entry is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Enter only the final results of the vote.
    /// </summary>
    FinalResults,

    /// <summary>
    /// Enter detailed vote results.
    /// </summary>
    Detailed,
}
