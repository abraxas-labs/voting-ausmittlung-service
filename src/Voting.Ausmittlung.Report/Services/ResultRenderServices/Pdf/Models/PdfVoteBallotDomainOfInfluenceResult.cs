// (c) Copyright 2024 by Abraxas Informatik AG
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

    [XmlElement("VoteAggregatedResult")]
    public PdfVoteDomainOfInfluenceBallotResult? AggregatedResult { get; set; }
}
