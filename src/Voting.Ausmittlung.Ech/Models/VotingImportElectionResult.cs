// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class VotingImportElectionResult : VotingImportPoliticalBusinessResult
{
    public VotingImportElectionResult(Guid electionId, string basisCountingCircleId, IReadOnlyCollection<VotingElectionBallot> ballots)
        : base(electionId, basisCountingCircleId)
    {
        Ballots = ballots;
    }

    public IReadOnlyCollection<VotingElectionBallot> Ballots { get; internal set; }
}
