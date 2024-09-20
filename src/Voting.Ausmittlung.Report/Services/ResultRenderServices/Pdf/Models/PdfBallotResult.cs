// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBallotResult
{
    public PdfBallot? Ballot { get; set; }

    public PdfPoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new PdfPoliticalBusinessCountOfVoters();

    [XmlElement("BallotQuestionResult")]
    public List<PdfBallotQuestionResult> QuestionResults { get; set; } = new List<PdfBallotQuestionResult>();

    [XmlElement("BallotTieBreakQuestionResult")]
    public List<PdfTieBreakQuestionResult> TieBreakQuestionResults { get; set; } = new List<PdfTieBreakQuestionResult>();
}
