// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteEndResult : PdfPoliticalBusinessEndResult
{
    [XmlElement("VoteBallotEndResult")]
    public List<PdfBallotEndResult>? BallotEndResults { get; set; }
}
