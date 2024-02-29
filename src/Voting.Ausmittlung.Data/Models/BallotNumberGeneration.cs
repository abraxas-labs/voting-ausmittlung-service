// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum BallotNumberGeneration
{
    /// <summary>
    /// Ballot number generation is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Restart the ballot numbers for each bundle.
    /// </summary>
    RestartForEachBundle,

    /// <summary>
    /// Use continuous numbers for all ballots, regardless of bundles.
    /// </summary>
    ContinuousForAllBundles,
}
