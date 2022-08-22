// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionUnmodifiedListResult : BaseEntity, IHasVoteCounts
{
    public ProportionalElectionResult Result { get; set; } = null!;

    public Guid ResultId { get; set; }

    public ProportionalElectionList List { get; set; } = null!;

    public Guid ListId { get; set; }

    public int EVotingVoteCount { get; set; }

    public int ConventionalVoteCount { get; set; }

    public int VoteCount
    {
        get => EVotingVoteCount + ConventionalVoteCount;
        private set
        {
            // empty setter to store the value in the database...
        }
    }
}
