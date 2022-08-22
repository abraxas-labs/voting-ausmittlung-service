// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionBallotGroup : BaseEntity
{
    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Position { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether all candidate counts of all ballot group entries are ok.
    /// </summary>
    public bool AllCandidateCountsOk { get; set; }

    public Guid MajorityElectionId { get; set; }

    public MajorityElection MajorityElection { get; set; } = null!;

    public ICollection<MajorityElectionBallotGroupEntry> Entries { get; set; }
        = new HashSet<MajorityElectionBallotGroupEntry>();

    public ICollection<MajorityElectionBallotGroupResult> BallotGroupResults { get; set; } =
        new HashSet<MajorityElectionBallotGroupResult>();
}
