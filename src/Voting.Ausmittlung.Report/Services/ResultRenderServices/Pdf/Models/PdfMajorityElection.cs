// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElection : PdfPoliticalBusiness
{
    public int NumberOfMandates { get; set; }

    public MajorityElectionMandateAlgorithm MandateAlgorithm { get; set; }

    [XmlElement("MajorityElectionResult")]
    public List<PdfMajorityElectionResult>? Results { get; set; }

    [XmlElement("MajorityElectionEndResult")]
    public PdfMajorityElectionEndResult? EndResult { get; set; }

    [XmlElement("MajorityElectionDomainOfInfluenceResult")]
    public List<PdfMajorityElectionDomainOfInfluenceResult>? DomainOfInfluenceResults { get; set; }

    [XmlElement("MajorityElectionAggregatedResult")]
    public PdfMajorityElectionDomainOfInfluenceResult? AggregatedDomainOfInfluenceResult { get; set; }
}
