// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.BiproportionalApportionment;

/// <summary>
/// Represents a single weight (element or proportional election list).
/// </summary>
public class Weight
{
    /// <summary>
    /// Gets the name of the weight.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the numeric value of the weight.
    /// </summary>
    public int VoteCount { get; init; }
}
