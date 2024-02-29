// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// Enumeration indicating where the voting is getting retrieved from.
/// </summary>
public enum VotingDataSource
{
    /// <summary>
    /// Conventional, physical source.
    /// </summary>
    Conventional,

    /// <summary>
    /// EVoting source.
    /// </summary>
    EVoting,
}
