// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum PoliticalBusinessSubType
{
    /// <summary>
    /// Political business sub type is unspecified, meaning it is a "standard" kind of political business.
    /// </summary>
    Unspecified,

    /// <summary>
    /// A vote with variant questions (on a single or multiple ballots).
    /// </summary>
    VoteVariantBallot,
}
