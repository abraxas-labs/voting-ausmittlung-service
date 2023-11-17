// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBallotQuestionResultSubTotal
{
    [XmlElement("CountOfAnswerTotalYes")]
    public int TotalCountOfAnswerYes { get; set; }

    [XmlElement("CountOfAnswerTotalNo")]
    public int TotalCountOfAnswerNo { get; set; }

    [XmlElement("CountOfAnswerTotalUnspecified")]
    public int TotalCountOfAnswerUnspecified { get; set; }

    public int CountOfAnswerTotal { get; set; }

    public decimal PercentageYes { get; set; }

    public decimal PercentageNo { get; set; }
}
