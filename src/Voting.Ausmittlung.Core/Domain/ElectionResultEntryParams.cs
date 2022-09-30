// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public abstract class ElectionResultEntryParams
{
    /// <summary>
    /// Gets or sets the ballot bundle size. When entering results, multiple ballots are put together in a bundle (to identify them better etc.).
    /// </summary>
    public int BallotBundleSize { get; set; }

    /// <summary>
    /// Gets or sets the ballot bundle sample size. When ballot bundles are used when entering results, some of the ballots must be checked for their correctness.
    /// This configures the amount of ballots that must be sampled per ballot bundle.
    /// </summary>
    public int BallotBundleSampleSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ballot bundle numbers are automatically generated by the system or if users enter the number manually.
    /// </summary>
    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    /// <summary>
    /// Gets or sets the number generation strategy for ballots inside ballot bundles.
    /// </summary>
    public BallotNumberGeneration BallotNumberGeneration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the count of empty votes is calculated automatically by the system or
    /// if it must be entered manually by the user (as a kind of "double check").
    /// </summary>
    public bool AutomaticEmptyVoteCounting { get; set; }
}
