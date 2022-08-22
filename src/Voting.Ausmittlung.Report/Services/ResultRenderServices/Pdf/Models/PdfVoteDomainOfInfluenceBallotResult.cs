// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteDomainOfInfluenceBallotResult : PdfDomainOfInfluenceResult
{
    [XmlElement("VoteCountingCircleResult")]
    public List<PdfVoteCountingCircleBallotResult> Results { get; set; } = new List<PdfVoteCountingCircleBallotResult>();

    [XmlElement("BallotQuestionResult")]
    public List<PdfBallotQuestionResult>? QuestionResults { get; set; }

    [XmlElement("TieBreakQuestionResult")]
    public List<PdfTieBreakQuestionResult>? TieBreakQuestionResults { get; set; }
}
