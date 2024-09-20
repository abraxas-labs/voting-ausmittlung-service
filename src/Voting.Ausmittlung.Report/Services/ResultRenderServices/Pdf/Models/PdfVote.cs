// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVote : PdfPoliticalBusiness
{
    [XmlElement("VoteEndResult")]
    public PdfVoteEndResult? EndResult { get; set; }

    [XmlElement("VoteResult")]
    public List<PdfVoteResult>? Results { get; set; }

    [XmlElement("VoteBallotDomainOfInfluenceResult")]
    public List<PdfVoteBallotDomainOfInfluenceResult>? DomainOfInfluenceBallotResults { get; set; }

    public string InternalDescription { get; set; } = string.Empty;
}
