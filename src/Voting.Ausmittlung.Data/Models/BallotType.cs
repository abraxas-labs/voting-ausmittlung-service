// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum BallotType
{
    /// <summary>
    /// Ballot number generation is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Ballot type standard.
    /// </summary>
    StandardBallot,

    /// <summary>
    /// Ballot type variants.
    /// </summary>
    VariantsBallot,
}
