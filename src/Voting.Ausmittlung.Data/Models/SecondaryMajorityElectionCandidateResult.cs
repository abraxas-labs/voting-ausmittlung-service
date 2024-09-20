// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionCandidateResult : MajorityElectionCandidateResultBase
{
    public SecondaryMajorityElectionResult ElectionResult { get; set; } = null!;

    public Guid ElectionResultId { get; set; }

    public SecondaryMajorityElectionCandidate Candidate { get; set; } = null!;

    public override int CandidatePosition => Candidate.Position;

    public ICollection<SecondaryMajorityElectionWriteInMapping> WriteInMappings { get; set; }
        = new HashSet<SecondaryMajorityElectionWriteInMapping>();
}
