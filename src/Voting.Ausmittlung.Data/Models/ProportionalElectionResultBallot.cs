// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionResultBallot : PrimaryElectionResultBallot
{
    public ProportionalElectionResultBundle Bundle { get; set; } = null!;

    public Guid BundleId { get; set; }

    public ICollection<ProportionalElectionResultBallotCandidate> BallotCandidates { get; set; }
        = new HashSet<ProportionalElectionResultBallotCandidate>();

    public List<ProportionalElectionResultBallotLog> Logs { get; set; } = new();
}
