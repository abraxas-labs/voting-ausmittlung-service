// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteBallotDomainOfInfluenceResult
{
    public PdfBallot? Ballot { get; set; }

    [XmlElement("VoteDomainOfInfluenceResult")]
    public List<PdfVoteDomainOfInfluenceBallotResult> Results { get; set; } = new List<PdfVoteDomainOfInfluenceBallotResult>();

    [XmlElement("VoteResult")]
    public PdfVoteDomainOfInfluenceBallotResult? NotAssignableResult { get; set; }
}
