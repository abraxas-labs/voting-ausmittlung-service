// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteResult : PdfCountingCircleResult
{
    [XmlElement("BallotResult")]
    public List<PdfBallotResult> Results { get; set; } = new List<PdfBallotResult>();
}
