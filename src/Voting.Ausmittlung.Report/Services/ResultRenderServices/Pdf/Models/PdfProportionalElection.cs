// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElection : PdfPoliticalBusiness
{
    public int NumberOfMandates { get; set; }

    [XmlElement("ProportionalElectionResult")]
    public List<PdfProportionalElectionResult> Results { get; set; } = new List<PdfProportionalElectionResult>();

    [XmlElement("ProportionalElectionEndResult")]
    public PdfProportionalElectionEndResult? EndResult { get; set; }
}
