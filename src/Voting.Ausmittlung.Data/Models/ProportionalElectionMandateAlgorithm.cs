// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum ProportionalElectionMandateAlgorithm
{
    /// <summary>
    /// Proportional election mandate algorithm is unspecified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The Hagenbach-Bischoff algorithm.
    /// </summary>
    HagenbachBischoff = 1,

    /// <summary>
    /// <para>The Double Proportional algorithm for N domain of influences with a 5% domain of influence quorum or 3% total quorum.</para>
    /// (Ex: Kantonratswahl).
    /// </summary>
    DoubleProportionalNDois5DoiOr3TotQuorum = 4,

    /// <summary>
    /// <para>The Double Proportional algorithm for N domain of influences with a 5% domain of influence quorum.</para>
    /// (Ex: Gemeindeparlamentswahl Stadt ZH).
    /// </summary>
    DoubleProportionalNDois5DoiQuorum = 5,

    /// <summary>
    /// <para>The Double Proportional algorithm for 1 domain of influence and no quorum.</para>
    /// (Ex: Gemeindeparlamentswahl).
    /// </summary>
    DoubleProportional1Doi0DoiQuorum = 6,
}
