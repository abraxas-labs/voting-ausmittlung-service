// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum BallotBundleState
{
    /// <summary>
    /// Ballot bundle state is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Ballot bundle state is in progress.
    /// </summary>
    InProcess,

    /// <summary>
    /// Ballot bundle state is in correction.
    /// </summary>
    InCorrection,

    /// <summary>
    /// Ballot bundle state is ready for review.
    /// </summary>
    ReadyForReview,

    /// <summary>
    /// Ballot bundle state is reviewed.
    /// </summary>
    Reviewed,

    /// <summary>
    /// Ballot bundle state is deleted.
    /// </summary>
    Deleted,
}
