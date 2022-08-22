// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionUnionEntry : BaseEntity
{
    public Guid MajorityElectionUnionId { get; set; }

    public MajorityElectionUnion MajorityElectionUnion { get; set; } = null!; // set by ef

    public Guid MajorityElectionId { get; set; }

    public MajorityElection MajorityElection { get; set; } = null!; // set by ef
}
