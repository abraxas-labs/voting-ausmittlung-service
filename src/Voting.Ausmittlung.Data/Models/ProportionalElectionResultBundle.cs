// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionResultBundle : ElectionBundle<ProportionalElectionResult>
{
    public ProportionalElectionList? List { get; set; }

    public Guid? ListId { get; set; }

    public ICollection<ProportionalElectionResultBallot> Ballots { get; set; } =
        new HashSet<ProportionalElectionResultBallot>();
}
