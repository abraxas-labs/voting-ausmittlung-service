// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionListUnionEntry : BaseEntity
{
    public Guid ProportionalElectionListId { get; set; }

    public ProportionalElectionList ProportionalElectionList { get; set; } = null!; // set by ef

    public Guid ProportionalElectionListUnionId { get; set; }

    public ProportionalElectionListUnion ProportionalElectionListUnion { get; set; } = null!; // set by ef
}
