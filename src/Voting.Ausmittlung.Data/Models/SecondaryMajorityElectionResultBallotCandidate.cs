// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionResultBallotCandidate : MajorityElectionResultBallotCandidateBase
{
    public SecondaryMajorityElectionCandidate Candidate { get; set; } = null!;

    public SecondaryMajorityElectionResultBallot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }
}
