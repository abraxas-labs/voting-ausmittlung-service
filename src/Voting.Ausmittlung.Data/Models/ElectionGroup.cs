// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ElectionGroup : BaseEntity
{
    public string Description { get; set; } = string.Empty;

    public int Number { get; set; }

    // Currently, only majority elections as both primary and secondary elections are supported
    public Guid PrimaryMajorityElectionId { get; set; }

    public MajorityElection PrimaryMajorityElection { get; set; } = null!; // set by EF

    public ICollection<SecondaryMajorityElection> SecondaryMajorityElections { get; set; } =
        new HashSet<SecondaryMajorityElection>();

    public int CountOfSecondaryElections { get; set; }
}
