// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// Indicates how to map a write in (in german: manuell erfasster Kandidat).
/// </summary>
public enum MajorityElectionWriteInMappingTarget
{
    /// <summary>
    /// The mapping target is not specified by the user yet.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Map the write in to "individual" (in german: Vereinzelte).
    /// </summary>
    Individual,

    /// <summary>
    /// Map the write in to an existing candidate.
    /// </summary>
    Candidate,

    /// <summary>
    /// Map the write in to an empty vote (in german: Leere Stimmabgabe).
    /// </summary>
    Empty,

    /// <summary>
    /// Map the write in to an invalid vote (in german: Ungültige Stimmabgabe).
    /// </summary>
    Invalid,

    /// <summary>
    /// Map the whole ballot of the write in as invalid (in german: Ungültiger Wahlzettel).
    /// This may happen when the write in contains insults or violates the electoral secrecy.
    /// </summary>
    InvalidBallot,
}
