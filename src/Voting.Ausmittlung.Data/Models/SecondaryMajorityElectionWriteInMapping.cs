// (c) Copyright by Abraxas Informatik AG
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

    public ResultImport? Import { get; set; }

    public override Guid? CandidateId => CandidateResult?.CandidateId;

    public override Guid PoliticalBusinessId => Result.SecondaryMajorityElectionId;
}
