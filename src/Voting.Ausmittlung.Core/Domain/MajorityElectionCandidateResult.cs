// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class MajorityElectionCandidateResult
{
    public Guid CandidateId { get; set; }

    /// <summary>
    /// Gets or sets the amount of votes a candidate has received.
    /// </summary>
    public int? VoteCount { get; set; }
}
