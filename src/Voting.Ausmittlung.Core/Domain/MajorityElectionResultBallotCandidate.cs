// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class MajorityElectionResultBallotCandidate
{
    public Guid CandidateId { get; set; }

    public int Position { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this candidate is on the ballot group originally or if the ballot was modified to add this candidate.
    /// </summary>
    public bool OnBallotGroup { get; set; }
}
