// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteResultBallot
{
    public int Number { get; set; }

    [XmlElement("QuestionAnswer")]
    public List<PdfVoteResultBallotQuestionAnswer> QuestionAnswers { get; set; } = new();

    [XmlElement("TieBreakQuestionAnswer")]
    public List<PdfVoteResultBallotTieBreakQuestionAnswer> TieBreakQuestionAnswers { get; set; } = new();
}
