// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Domain;

public class SecondaryMajorityElectionResultBallot
{
    public Guid SecondaryMajorityElectionId { get; set; }

    /// <summary>
    /// Gets or sets the amount of empty votes.
    /// </summary>
    public int? EmptyVoteCount { get; set; }

    /// <summary>
    /// Gets or sets the amount of votes for individuals (in german: Vereinzelte).
    /// </summary>
    public int IndividualVoteCount { get; set; }

    public List<Guid> SelectedCandidateIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the amount of invalid votes.
    /// </summary>
    public int InvalidVoteCount { get; set; }
}
