// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class MajorityElectionBallotGroupResult
{
    public Guid BallotGroupId { get; set; }

    /// <summary>
    /// Gets or sets the amount of votes a ballot group has received.
    /// </summary>
    public int VoteCount { get; set; }
}
