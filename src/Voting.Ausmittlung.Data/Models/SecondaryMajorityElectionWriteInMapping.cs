// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionWriteInMapping : MajorityElectionWriteInMappingBase
{
    public Guid ResultId { get; set; }

    public SecondaryMajorityElectionResult Result { get; set; } = null!;

    public Guid? CandidateResultId { get; set; }

    public SecondaryMajorityElectionCandidateResult? CandidateResult { get; set; }

    public ICollection<SecondaryMajorityElectionWriteInBallotPosition> BallotPositions { get; set; }
        = new HashSet<SecondaryMajorityElectionWriteInBallotPosition>();

    public override Guid? CandidateId => CandidateResult?.CandidateId;
}
