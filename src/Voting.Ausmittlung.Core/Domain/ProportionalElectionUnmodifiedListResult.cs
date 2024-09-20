// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class ProportionalElectionUnmodifiedListResult
{
    public Guid ListId { get; set; }

    /// <summary>
    /// Gets or sets the amount of votes this unmodified list has received.
    /// </summary>
    public int VoteCount { get; set; }
}
