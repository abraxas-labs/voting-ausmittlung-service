// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionWriteInMapping : MajorityElectionWriteInMappingBase
{
    public Guid ResultId { get; set; }

    public MajorityElectionResult Result { get; set; } = null!;

    public Guid? CandidateResultId { get; set; }

    public MajorityElectionCandidateResult? CandidateResult { get; set; }

    public ICollection<MajorityElectionWriteInBallotPosition> BallotPositions { get; set; }
        = new HashSet<MajorityElectionWriteInBallotPosition>();

    public override Guid? CandidateId => CandidateResult?.CandidateId;
}
