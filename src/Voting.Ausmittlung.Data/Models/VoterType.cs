// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// Enumeration for the VoterType (eCH-0110).
/// </summary>
public enum VoterType
{
    /// <summary>
    /// Voter type is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Voter is Swiss.
    /// </summary>
    Swiss,

    /// <summary>
    /// Voter is Swiss but living abroad.
    /// </summary>
    SwissAbroad,

    /// <summary>
    /// Voter is foreigner.
    /// </summary>
    Foreigner,

    /// <summary>
    /// Voter is minor.
    /// </summary>
    Minor,
}
