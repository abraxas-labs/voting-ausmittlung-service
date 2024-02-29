// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionCandidateResultBase : BaseEntity, IHasNullableConventionalVoteCounts
{
    public Guid CandidateId { get; set; }

    public abstract int CandidatePosition { get; }

    public int? ConventionalVoteCount { get; set; }

    public int EVotingExclWriteInsVoteCount { get; set; }

    public int EVotingWriteInsVoteCount { get; set; }

    public int EVotingInclWriteInsVoteCount => EVotingExclWriteInsVoteCount + EVotingWriteInsVoteCount;

    public int VoteCount
    {
        get => ConventionalVoteCount.GetValueOrDefault() + EVotingInclWriteInsVoteCount;
        private set
        {
            // empty setter to store the value in the database...
        }
    }
}
