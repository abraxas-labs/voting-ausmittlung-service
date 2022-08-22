// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionUnionListEntry : BaseEntity
{
    public Guid ProportionalElectionUnionListId { get; set; }

    public ProportionalElectionUnionList ProportionalElectionUnionList { get; set; } = null!; // set by ef

    public Guid ProportionalElectionListId { get; set; }

    public ProportionalElectionList ProportionalElectionList { get; set; } = null!; // set by ef
}
