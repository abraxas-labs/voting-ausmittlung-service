// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum ProportionalElectionCandidateEndResultState
{
    /// <summary>
    /// Proportional election candidate end result state is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Pendent.
    /// </summary>
    Pending,

    /// <summary>
    /// Gewählt.
    /// </summary>
    Elected,

    /// <summary>
    /// Nicht gewählt.
    /// </summary>
    NotElected,
}
