// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionWriteInBallot : MajorityElectionWriteInBallotBase
{
    public Guid ResultId { get; set; }

    public SecondaryMajorityElectionResult Result { get; set; } = null!;

    public ICollection<SecondaryMajorityElectionWriteInBallotPosition> WriteInPositions { get; set; } =
        new HashSet<SecondaryMajorityElectionWriteInBallotPosition>();
}
