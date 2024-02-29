// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBallotQuestionResult
{
    [XmlElement("BallotQuestion")]
    public PdfBallotQuestion? Question { get; set; }

    [XmlElement("CountOfAnswerTotalYes")]
    public int TotalCountOfAnswerYes { get; set; }

    [XmlElement("CountOfAnswerTotalNo")]
    public int TotalCountOfAnswerNo { get; set; }

    [XmlElement("CountOfAnswerTotalUnspecified")]
    public int TotalCountOfAnswerUnspecified { get; set; }

    public int CountOfAnswerTotal { get; set; }

    public decimal PercentageYes { get; set; }

    public decimal PercentageNo { get; set; }

    public PdfBallotQuestionResultSubTotal? EVotingSubTotal { get; set; }

    public PdfBallotQuestionResultSubTotal? ConventionalSubTotal { get; set; }
}
