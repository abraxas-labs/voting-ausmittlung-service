// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionResultBundle : PdfPoliticalBusinessBundle
{
    public PdfProportionalElectionSimpleList? List { get; set; }

    [XmlElement("ProportionalElectionResultBallot")]
    public List<PdfProportionalElectionResultBallot> Ballots { get; set; } = new();
}
