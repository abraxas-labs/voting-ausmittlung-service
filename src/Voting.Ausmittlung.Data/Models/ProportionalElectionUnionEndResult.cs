// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionUnionEndResult : BaseEntity
{
    public Guid ProportionalElectionUnionId { get; set; }

    public ProportionalElectionUnion ProportionalElectionUnion { get; set; } = null!;

    public int CountOfDoneElections { get; set; }

    public int TotalCountOfElections { get; set; }

    public bool AllElectionsDone => TotalCountOfElections > 0 && CountOfDoneElections == TotalCountOfElections;

    public bool Finalized { get; set; }
}
