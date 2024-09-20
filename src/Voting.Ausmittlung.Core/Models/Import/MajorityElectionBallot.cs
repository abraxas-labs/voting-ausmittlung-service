// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models.Import;

public class MajorityElectionBallot
{
    public MajorityElectionBallot(int emptyVoteCount)
    {
        EmptyVoteCount = emptyVoteCount;
    }

    /// <summary>
    /// Gets the candidate ids on the ballot.
    /// </summary>
    public HashSet<Guid> CandidateIds { get; } = new();

    /// <summary>
    /// Gets the write ins on the ballot.
    /// </summary>
    public List<string> WriteIns { get; } = new();

    /// <summary>
    /// Gets the write in mapping ids.
    /// </summary>
    public List<Guid> WriteInMappingIds { get; } = new();

    /// <summary>
    /// Gets or sets the empty votes on the ballot.
    /// </summary>
    public int EmptyVoteCount { get; set; }

    /// <summary>
    /// Gets or sets the invalid votes on the ballot (ex. a candidate id was provided twice).
    /// </summary>
    public int InvalidVoteCount { get; set; }
}
