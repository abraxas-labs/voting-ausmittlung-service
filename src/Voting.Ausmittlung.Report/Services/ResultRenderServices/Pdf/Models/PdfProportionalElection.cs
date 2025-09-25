// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElection : PdfPoliticalBusiness
{
    public int NumberOfMandates { get; set; }

    public ProportionalElectionMandateAlgorithm MandateAlgorithm { get; set; }

    [XmlElement("ProportionalElectionResult")]
    public List<PdfProportionalElectionResult> Results { get; set; } = new List<PdfProportionalElectionResult>();

    [XmlElement("ProportionalElectionEndResult")]
    public PdfProportionalElectionEndResult? EndResult { get; set; }

    [XmlElement("ProportionalElectionDoubleProportionalResult")]
    public PdfDoubleProportionalResult? DoubleProportionalResult { get; set; }
}
