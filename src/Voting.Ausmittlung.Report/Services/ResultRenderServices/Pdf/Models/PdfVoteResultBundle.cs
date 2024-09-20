// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteResultBundle : PdfPoliticalBusinessBundle
{
    [XmlElement("VoteResultBallot")]
    public List<PdfVoteResultBallot> Ballots { get; set; } = new();
}
