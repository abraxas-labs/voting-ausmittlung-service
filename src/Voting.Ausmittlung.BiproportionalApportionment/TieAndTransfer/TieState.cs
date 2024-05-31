// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

/// <summary>
/// Describe the tie state of a certain apportionment (proportional election list number of mandates).
/// </summary>
public enum TieState
{
    /// <summary>
    /// Unique solution.
    /// </summary>
    Unique,

    /// <summary>
    /// Positive tie (quotient at the upper boundary). The value was rounded down, which means n and n+1 are allowed.
    /// </summary>
    Positive,

    /// <summary>
    /// Negative tie (quotient at the lower boundary). The value was rounded up, which means n-1 and n are allowed.
    /// </summary>
    Negative,
}
