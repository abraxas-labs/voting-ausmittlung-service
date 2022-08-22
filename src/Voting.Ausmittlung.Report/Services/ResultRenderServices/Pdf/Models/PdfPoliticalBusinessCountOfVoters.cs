// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfPoliticalBusinessCountOfVoters
{
    public decimal VoterParticipation { get; set; }

    public int TotalReceivedBallots { get; set; }

    public int TotalUnaccountedBallots { get; set; }

    [XmlElement("CountOfAccountedBallots")] // legacy name
    public int TotalAccountedBallots { get; set; }

    [XmlElement("CountOfInvalidBallots")] // legacy name
    public int ConventionalInvalidBallots { get; set; }

    [XmlElement("CountOfBlankBallots")] // legacy name
    public int ConventionalBlankBallots { get; set; }
}
