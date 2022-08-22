// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionCandidateEndResult : MajorityElectionCandidateEndResultBase
{
    public Guid MajorityElectionEndResultId { get; set; }

    public MajorityElectionEndResult MajorityElectionEndResult { get; set; } = null!; // set by ef

    public MajorityElectionCandidate Candidate { get; set; } = null!; // set by ef
}
