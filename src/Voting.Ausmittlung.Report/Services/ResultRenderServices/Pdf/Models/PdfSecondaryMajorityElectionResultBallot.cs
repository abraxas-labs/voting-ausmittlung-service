// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfSecondaryMajorityElectionResultBallot
{
    public int EmptyVoteCount { get; set; }

    public int IndividualVoteCount { get; set; }

    [XmlElement("SecondaryMajorityElectionCandidate")]
    public List<PdfMajorityElectionCandidate> Candidates { get; set; } = new();
}
