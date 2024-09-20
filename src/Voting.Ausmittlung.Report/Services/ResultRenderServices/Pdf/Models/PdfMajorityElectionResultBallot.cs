// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionResultBallot
{
    public int Number { get; set; }

    public int EmptyVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }

    public int IndividualVoteCount { get; set; }

    [XmlElement("MajorityElectionCandidate")]
    public List<PdfMajorityElectionCandidate> Candidates { get; set; } = new();

    [XmlElement("SecondaryMajorityElectionResultBallot")]
    public List<PdfSecondaryMajorityElectionResultBallot> SecondaryMajorityElectionBallots { get; set; } = new();
}
