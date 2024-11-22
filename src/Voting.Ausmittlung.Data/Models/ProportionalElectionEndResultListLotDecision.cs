// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionEndResultListLotDecision : BaseEntity
{
    public Guid ProportionalElectionEndResultId { get; set; }

    public ProportionalElectionEndResult ProportionalElectionEndResult { get; set; } = null!;

    public ICollection<ProportionalElectionEndResultListLotDecisionEntry> Entries { get; set; }
        = new HashSet<ProportionalElectionEndResultListLotDecisionEntry>();
}
