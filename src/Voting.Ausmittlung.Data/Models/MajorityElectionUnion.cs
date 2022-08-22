// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionUnion : PoliticalBusinessUnion
{
    public ICollection<MajorityElectionUnionEntry> MajorityElectionUnionEntries { get; set; }
        = new HashSet<MajorityElectionUnionEntry>();

    public override PoliticalBusinessUnionType Type => PoliticalBusinessUnionType.MajorityElection;

    [NotMapped]
    public override IEnumerable<PoliticalBusiness> PoliticalBusinesses
        => MajorityElectionUnionEntries.Select(x => x.MajorityElection);
}
