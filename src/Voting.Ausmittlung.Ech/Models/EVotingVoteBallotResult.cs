// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingVoteBallotResult
{
    public EVotingVoteBallotResult(Guid ballotId, IReadOnlyCollection<EVotingVoteBallot> ballots)
    {
        BallotId = ballotId;
        Ballots = ballots;
    }

    public Guid BallotId { get; }

    public IReadOnlyCollection<EVotingVoteBallot> Ballots { get; internal set; }
}
