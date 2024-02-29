// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class ElectionCandidateEndResult : BaseEntity
{
    public Guid CandidateId { get; set; }

    public int Rank { get; set; }

    public abstract int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the lot decision was applied yet by a user.
    /// </summary>
    public bool LotDecision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this candidate end result can have a lot decision.
    /// </summary>
    public bool LotDecisionEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this candidate end result needs to have a lot decision.
    /// For example this is true if two candidates has the same amount of votes
    /// and are in the ranks of the available number of mandates.
    /// It is false if no lot decision is available for this candidate end result or
    /// it is not required.
    /// </summary>
    public bool LotDecisionRequired { get; set; }
}
