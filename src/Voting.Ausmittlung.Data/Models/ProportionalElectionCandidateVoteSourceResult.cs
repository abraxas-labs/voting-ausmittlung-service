// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionCandidateVoteSourceResult : BaseEntity, IHasVoteCounts
{
    public ProportionalElectionCandidateResult CandidateResult { get; set; } = null!;

    public Guid CandidateResultId { get; set; }

    public ProportionalElectionList? List { get; set; }

    public Guid? ListId { get; set; }

    public int ECountingVoteCount { get; set; }

    public int EVotingVoteCount { get; set; }

    public int ConventionalVoteCount { get; set; }

    public int VoteCount => ConventionalVoteCount + EVotingVoteCount + ECountingVoteCount;

    public void MoveECountingToConventional()
    {
        ConventionalVoteCount += ECountingVoteCount;
        ECountingVoteCount = 0;
    }
}
