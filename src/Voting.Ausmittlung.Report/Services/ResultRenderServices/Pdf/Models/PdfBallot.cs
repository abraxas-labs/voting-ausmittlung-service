// (c) Copyright 2022 by Abraxas Informatik AG
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

    public string Description { get; set; } = string.Empty;

    public BallotType BallotType { get; set; }

    [XmlElement("BallotQuestion")]
    public List<PdfBallotQuestion>? BallotQuestions { get; set; }

    [XmlElement("TieBreakQuestion")]
    public List<PdfTieBreakQuestion>? TieBreakQuestions { get; set; }
}
