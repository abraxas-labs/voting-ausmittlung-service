// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class VoteBallotResultsCountOfVoters
{
    public Guid BallotId { get; set; }

    public PoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new();
}
