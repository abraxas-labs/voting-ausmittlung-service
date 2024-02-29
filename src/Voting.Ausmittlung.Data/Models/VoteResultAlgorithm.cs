// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum VoteResultAlgorithm
{
    /// <summary>
    /// Vote result algorithm is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The vote is accepted if more persons voted for it than against it.
    /// </summary>
    PopularMajority,

    /// <summary>
    /// The vote is accepted only if all counting circles accepted it.
    /// </summary>
    CountingCircleUnanimity,

    /// <summary>
    /// The vote is accepted if the majority of the counting circles accepted it.
    /// </summary>
    CountingCircleMajority,
}
