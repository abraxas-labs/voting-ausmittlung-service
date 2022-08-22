// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionDomainOfInfluenceResult : PdfDomainOfInfluenceResult
{
    public int IndividualVoteCount { get; set; }

    public int EmptyVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }

    [XmlElement("MajorityElectionCandidateResult")]
    public List<PdfMajorityElectionCandidateResult>? CandidateResults { get; set; }

    [XmlElement("MajorityElectionResult")]
    public List<PdfMajorityElectionResult>? Results { get; set; }
}
