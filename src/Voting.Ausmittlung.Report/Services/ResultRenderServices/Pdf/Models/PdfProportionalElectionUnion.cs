// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionUnion : PdfPoliticalBusinessUnion
{
    [XmlElement("ProportionalElectionUnionEndResult")]
    public PdfProportionalElectionUnionEndResult? EndResult { get; set; }
}
