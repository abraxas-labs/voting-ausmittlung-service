// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum DomainOfInfluenceType
{
    /// <summary>
    /// Domain of influence type is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Schweiz / Bund.
    /// </summary>
    Ch,

    /// <summary>
    /// Kanton.
    /// </summary>
    Ct,

    /// <summary>
    /// Bezirk.
    /// </summary>
    Bz,

    /// <summary>
    /// Gemeinde.
    /// </summary>
    Mu,

    /// <summary>
    /// Stadtkreis.
    /// </summary>
    Sk,

    /// <summary>
    /// Schulgemeinde.
    /// </summary>
    Sc,

    /// <summary>
    /// Kirchgemeinde.
    /// </summary>
    Ki,

    /// <summary>
    /// Ortsbürgergemeinde.
    /// </summary>
    Og,

    /// <summary>
    /// Koprorationen.
    /// </summary>
    Ko,

    /// <summary>
    /// Andere.
    /// </summary>
    An,
}
