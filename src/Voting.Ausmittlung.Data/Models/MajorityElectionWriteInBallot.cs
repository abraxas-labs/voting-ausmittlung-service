// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionWriteInBallot : MajorityElectionWriteInBallotBase
{
    public Guid ResultId { get; set; }

    public MajorityElectionResult Result { get; set; } = null!;

    public ICollection<MajorityElectionWriteInBallotPosition> WriteInPositions { get; set; } =
        new HashSet<MajorityElectionWriteInBallotPosition>();

    public bool MapsToInvalidBallot()
        => WriteInPositions.Any(p => p.Target == MajorityElectionWriteInMappingTarget.InvalidBallot);

    public bool AllPositionsEmpty()
        => WriteInPositions.All(p => p.Target == MajorityElectionWriteInMappingTarget.Empty);
}
