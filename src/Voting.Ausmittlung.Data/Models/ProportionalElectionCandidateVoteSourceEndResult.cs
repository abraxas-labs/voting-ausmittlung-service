// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionCandidateVoteSourceEndResult : BaseEntity, IHasVoteCounts
{
    public ProportionalElectionCandidateEndResult CandidateResult { get; set; } = null!;

    public Guid CandidateResultId { get; set; }

    public ProportionalElectionList? List { get; set; }

    public Guid? ListId { get; set; }

    public int EVotingVoteCount { get; set; }

    public int ConventionalVoteCount { get; set; }

    public int VoteCount => EVotingVoteCount + ConventionalVoteCount;
}
