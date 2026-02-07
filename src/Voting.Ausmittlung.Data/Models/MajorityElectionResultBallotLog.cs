// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultBallotLog : PoliticalBusinessResultBallotLog
{
    public MajorityElectionResultBallot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }
}
