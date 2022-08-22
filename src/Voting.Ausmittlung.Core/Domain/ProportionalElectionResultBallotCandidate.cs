// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class ProportionalElectionResultBallotCandidate
{
    public Guid CandidateId { get; set; }

    public int Position { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this candidate is on the list originally or if the list was modified to add this candidate.
    /// </summary>
    public bool OnList { get; set; }
}
