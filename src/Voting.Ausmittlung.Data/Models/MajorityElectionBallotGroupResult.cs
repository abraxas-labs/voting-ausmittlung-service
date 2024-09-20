// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionBallotGroupResult : BaseEntity
{
    public MajorityElectionResult ElectionResult { get; set; } = null!;

    public Guid ElectionResultId { get; set; }

    public MajorityElectionBallotGroup BallotGroup { get; set; } = null!;

    public Guid BallotGroupId { get; set; }

    public int VoteCount { get; set; }
}
