// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum MajorityElectionCandidateEndResultState
{
    /// <summary>
    /// Majority election candidate end result state is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Pendent.
    /// </summary>
    Pending,

    /// <summary>
    /// Absolutes Mehr erreicht und gewählt.
    /// </summary>
    AbsoluteMajorityAndElected,

    /// <summary>
    /// Absolutes Mehr erreicht / als überzählig ausgeschieden.
    /// </summary>
    AbsoluteMajorityAndNotElected,

    /// <summary>
    /// Absolutes Mehr verpasst / nicht gewählt.
    /// </summary>
    NoAbsoluteMajorityAndNotElectedButRankOk,

    /// <summary>
    /// Gewählt.
    /// </summary>
    Elected,

    /// <summary>
    /// Nicht gewählt.
    /// </summary>
    NotElected,

    /// <summary>
    /// Nicht wählbar (Vereinzelte).
    /// </summary>
    NotEligible,
}
