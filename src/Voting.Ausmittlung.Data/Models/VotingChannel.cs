// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum VotingChannel
{
    /// <summary>
    /// Voting channel is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Urne.
    /// </summary>
    BallotBox,

    /// <summary>
    /// Brieflich.
    /// </summary>
    ByMail,

    /// <summary>
    /// Elektronisch.
    /// </summary>
    EVoting,

    /// <summary>
    /// Papier / vorzeitig.
    /// </summary>
    Paper,
}
