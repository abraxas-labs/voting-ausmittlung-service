// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum ProportionalElectionMandateAlgorithm
{
    /// <summary>
    /// Proportional election mandate algorithm is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The Hagenbach-Bischoff algorithm.
    /// </summary>
    HagenbachBischoff,

    /// <summary>
    /// The Doppelter Pukelsheim algorithm with 5% quorum.
    /// </summary>
    DoppelterPukelsheim5Quorum,

    /// <summary>
    /// The Doppelter Pukelsheim algorithm with 0% quorum.
    /// </summary>
    DoppelterPukelsheim0Quorum,
}
