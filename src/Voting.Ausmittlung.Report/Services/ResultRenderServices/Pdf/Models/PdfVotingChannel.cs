// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public enum PdfVotingChannel
{
    /// <summary>
    /// Unbekannt.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Urne.
    /// </summary>
    BallotBox = 1,

    /// <summary>
    /// Brieflich.
    /// </summary>
    ByMail = 2,

    /// <summary>
    /// Elektronisch.
    /// </summary>
    EVoting = 3,

    /// <summary>
    /// Papier / vorzeitig.
    /// </summary>
    Paper = 4,
}
