// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteCountingCircleBallotResult : PdfCountingCircleResult
{
    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

    [XmlElement("BallotQuestionResult")]
    public List<PdfBallotQuestionResult>? QuestionResults { get; set; }

    [XmlElement("TieBreakQuestionResult")]
    public List<PdfTieBreakQuestionResult>? TieBreakQuestionResults { get; set; }
}
