// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionWriteInBallotPositionBase : BaseEntity
{
    public Guid BallotId { get; set; }

    public Guid WriteInMappingId { get; set; }

    /// <summary>
    /// Gets or sets the target of this write in position.
    /// This may differ from the <see cref="MajorityElectionWriteInMapping"/> target, for example
    /// when the same candidate is listed multiple times on the same ballot.
    /// </summary>
    public MajorityElectionWriteInMappingTarget Target { get; set; }
}
