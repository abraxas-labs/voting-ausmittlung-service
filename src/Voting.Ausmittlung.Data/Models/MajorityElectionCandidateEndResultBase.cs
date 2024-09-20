﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionCandidateEndResultBase : ElectionCandidateEndResult, IHasVoteCounts
{
    public MajorityElectionCandidateEndResultState State { get; set; }

    public int ConventionalVoteCount { get; set; }

    public int EVotingVoteCount { get; set; }

    public override int VoteCount
    {
        get => ConventionalVoteCount + EVotingVoteCount;
        set
        {
            // empty setter to store the value in the database...
        }
    }
}
