// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum PoliticalBusinessUnionType
{
    /// <summary>
    /// Political business union type is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Union for proportional elections.
    /// </summary>
    ProportionalElection,

    /// <summary>
    /// Union for majority elections.
    /// </summary>
    MajorityElection,
}
