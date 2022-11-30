// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingVoteResult : EVotingPoliticalBusinessResult
{
    public EVotingVoteResult(Guid voteId, Guid basisCountingCircleId, IReadOnlyCollection<EVotingVoteBallotResult> ballotResults)
        : base(voteId, basisCountingCircleId)
    {
        BallotResults = ballotResults;
        PoliticalBusinessType = PoliticalBusinessType.Vote;
    }

    public IReadOnlyCollection<EVotingVoteBallotResult> BallotResults { get; internal set; }
}
