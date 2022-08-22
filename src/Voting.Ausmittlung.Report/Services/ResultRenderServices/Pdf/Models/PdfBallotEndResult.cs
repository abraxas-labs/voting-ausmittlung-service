// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBallotEndResult
{
    public PdfBallot? Ballot { get; set; }

    public PdfPoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new PdfPoliticalBusinessCountOfVoters();

    [XmlElement("BallotQuestionEndResult")]
    public List<PdfBallotQuestionEndResult> QuestionEndResults { get; set; } = new List<PdfBallotQuestionEndResult>();

    [XmlElement("BallotTieBreakQuestionEndResult")]
    public List<PdfTieBreakQuestionEndResult> TieBreakQuestionEndResults { get; set; } = new List<PdfTieBreakQuestionEndResult>();
}
