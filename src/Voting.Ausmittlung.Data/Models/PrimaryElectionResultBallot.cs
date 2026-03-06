// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class PrimaryElectionResultBallot : BaseEntity
{
    public int Number { get; set; }

    public int EmptyVoteCount { get; set; }

    public bool MarkedForReview { get; set; }

    /// <summary>
    /// Gets or sets the usually zero-based index. Older data may contain non-zero based numbers in here.
    /// Should only be used for ordering the ballot inside a bundle.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this ballot was modified while the bundle was in the ReadyForReview state.
    /// The value is reset when the bundle is sent back for corrections.
    /// </summary>
    public bool ModifiedDuringReview { get; set; }
}
