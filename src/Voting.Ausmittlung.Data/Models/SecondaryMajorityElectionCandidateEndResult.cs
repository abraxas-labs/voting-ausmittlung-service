// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionCandidateEndResult : MajorityElectionCandidateEndResultBase
{
    public SecondaryMajorityElectionCandidate Candidate { get; set; } = null!; // set by ef

    public Guid SecondaryMajorityElectionEndResultId { get; set; }

    public SecondaryMajorityElectionEndResult SecondaryMajorityElectionEndResult { get; set; } = null!; // set by ef
}
