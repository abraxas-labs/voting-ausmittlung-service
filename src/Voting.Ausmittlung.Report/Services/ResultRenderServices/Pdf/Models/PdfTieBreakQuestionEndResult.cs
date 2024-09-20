// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfTieBreakQuestionEndResult
{
    [XmlElement("BallotTieBreakQuestion")]
    public PdfTieBreakQuestion? Question { get; set; }

    [XmlElement("CountOfAnswerTotalQ1")]
    public int TotalCountOfAnswerQ1 { get; set; }

    [XmlElement("CountOfAnswerTotalQ2")]
    public int TotalCountOfAnswerQ2 { get; set; }

    [XmlElement("CountOfAnswerTotalUnspecified")]
    public int TotalCountOfAnswerUnspecified { get; set; }

    public int CountOfAnswerTotal { get; set; }

    public decimal PercentageQ1 { get; set; }

    public decimal PercentageQ2 { get; set; }

    public bool Q1Accepted { get; set; }

    public PdfTieBreakQuestionResultSubTotal? EVotingSubTotal { get; set; }

    public PdfTieBreakQuestionResultSubTotal? ConventionalSubTotal { get; set; }
}
