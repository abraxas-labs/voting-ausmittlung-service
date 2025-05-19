// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

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

    public VoteResultAlgorithm ResultAlgorithm { get; set; }

    public VoteType Type { get; set; }

    // The name of the domain of influence at the specified reporting level.
    // Only makes sense if the report is specific to a counting circle.
    public string? ReportingLevelName { get; set; }
}
