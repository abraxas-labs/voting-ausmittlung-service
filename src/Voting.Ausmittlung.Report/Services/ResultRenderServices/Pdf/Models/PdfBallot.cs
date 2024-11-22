// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBallot
{
    [XmlIgnore]
    public Guid Id { get; set; }

    public int Position { get; set; }

    public BallotType BallotType { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string OfficialDescription { get; set; } = string.Empty;

    [XmlElement("BallotQuestion")]
    public List<PdfBallotQuestion>? BallotQuestions { get; set; }

    [XmlElement("TieBreakQuestion")]
    public List<PdfTieBreakQuestion>? TieBreakQuestions { get; set; }
}
