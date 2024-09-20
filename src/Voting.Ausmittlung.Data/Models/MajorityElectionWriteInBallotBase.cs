// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionWriteInBallotBase : BaseEntity
{
    public List<Guid> CandidateIds { get; set; } = new();

    public int EmptyVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }
}
