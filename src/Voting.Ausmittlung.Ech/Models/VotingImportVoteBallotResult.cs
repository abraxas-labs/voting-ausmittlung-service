// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class VotingImportVoteBallotResult
{
    public VotingImportVoteBallotResult(Guid ballotId, IReadOnlyCollection<VotingVoteBallot> ballots)
    {
        BallotId = ballotId;
        Ballots = ballots;
    }

    public Guid BallotId { get; }

    public IReadOnlyCollection<VotingVoteBallot> Ballots { get; internal set; }
}
