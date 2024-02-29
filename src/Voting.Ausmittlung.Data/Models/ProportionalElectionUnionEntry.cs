// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionUnionEntry : BaseEntity
{
    public Guid ProportionalElectionUnionId { get; set; }

    public ProportionalElectionUnion ProportionalElectionUnion { get; set; } = null!; // set by ef

    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!; // set by ef
}
