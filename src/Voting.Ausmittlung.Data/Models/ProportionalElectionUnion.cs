// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionUnion : PoliticalBusinessUnion
{
    public ICollection<ProportionalElectionUnionEntry> ProportionalElectionUnionEntries { get; set; }
        = new HashSet<ProportionalElectionUnionEntry>();

    public ICollection<ProportionalElectionUnionList> ProportionalElectionUnionLists { get; set; }
        = new HashSet<ProportionalElectionUnionList>();

    public override PoliticalBusinessUnionType Type => PoliticalBusinessUnionType.ProportionalElection;

    public ProportionalElectionUnionEndResult? EndResult { get; set; }

    public DoubleProportionalResult? DoubleProportionalResult { get; set; }

    [NotMapped]
    public override IEnumerable<PoliticalBusiness> PoliticalBusinesses
        => ProportionalElectionUnionEntries.Select(x => x.ProportionalElection);
}
