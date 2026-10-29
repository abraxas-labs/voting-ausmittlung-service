// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultBundle : ElectionBundle<MajorityElectionResult>
{
    public MajorityElectionBallotGroup? BallotGroup { get; set; }

    public Guid? BallotGroupId { get; set; }

    public ICollection<MajorityElectionResultBallot> Ballots { get; set; } =
        new HashSet<MajorityElectionResultBallot>();

    public List<MajorityElectionResultBundleLog> Logs { get; set; } = new();
}
