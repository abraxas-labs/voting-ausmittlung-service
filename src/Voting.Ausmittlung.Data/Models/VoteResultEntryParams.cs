// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class VoteResultEntryParams
{
    public int BallotBundleSampleSizePercent { get; set; }

    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public VoteReviewProcedure ReviewProcedure { get; set; }
}
