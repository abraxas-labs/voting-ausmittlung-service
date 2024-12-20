// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class VoteResultEntryParams
{
    /// <summary>
    /// Gets or sets the percentage of ballots inside a ballot bundle that must be sampled for correctness.
    /// Note that this is the integer value of a percentage, ie. a value of 77 would mean 77%.
    /// </summary>
    public int BallotBundleSampleSizePercent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ballot bundle numbers are automatically generated by the system or if users enter the number manually.
    /// </summary>
    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public VoteReviewProcedure ReviewProcedure { get; set; }
}
