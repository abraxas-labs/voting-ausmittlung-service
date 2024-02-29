// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteCountingCircleResult : PdfCountingCircleResult
{
    public PdfPoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new PdfPoliticalBusinessCountOfVoters();

    public PdfContestDomainOfInfluenceDetails ContestDomainOfInfluenceDetails { get; set; } = new PdfContestDomainOfInfluenceDetails();

    [XmlElement("BallotQuestionResult")]
    public List<PdfBallotQuestionResult> QuestionResults { get; set; } = new List<PdfBallotQuestionResult>();

    [XmlElement("BallotTieBreakQuestionResult")]
    public List<PdfTieBreakQuestionResult> TieBreakQuestionResults { get; set; } = new List<PdfTieBreakQuestionResult>();
}
