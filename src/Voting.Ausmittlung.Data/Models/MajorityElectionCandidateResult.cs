// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionCandidateResult : MajorityElectionCandidateResultBase
{
    public MajorityElectionResult ElectionResult { get; set; } = null!;

    public Guid ElectionResultId { get; set; }

    public MajorityElectionCandidate Candidate { get; set; } = null!;

    public override int CandidatePosition => Candidate.Position;
}
