// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingElectionResult : EVotingPoliticalBusinessResult
{
    public EVotingElectionResult(Guid electionId, Guid basisCountingCircleId, IReadOnlyCollection<EVotingElectionBallot> ballots)
        : base(electionId, basisCountingCircleId)
    {
        Ballots = ballots;
    }

    public IReadOnlyCollection<EVotingElectionBallot> Ballots { get; internal set; }
}
