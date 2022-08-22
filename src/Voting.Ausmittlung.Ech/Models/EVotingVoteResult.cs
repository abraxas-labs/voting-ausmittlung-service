// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingVoteResult : EVotingPoliticalBusinessResult
{
    public EVotingVoteResult(Guid voteId, Guid basisCountingCircleId, IReadOnlyCollection<EVotingVoteBallotResult> ballotResults)
        : base(voteId, basisCountingCircleId)
    {
        BallotResults = ballotResults;
    }

    public IReadOnlyCollection<EVotingVoteBallotResult> BallotResults { get; internal set; }
}
