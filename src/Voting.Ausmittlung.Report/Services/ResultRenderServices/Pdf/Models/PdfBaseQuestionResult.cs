// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBaseQuestionResult
{
    [XmlIgnore]
    public int TotalCountOfAnswerYes { get; set; }

    [XmlElement("CountOfAnswerTotalYes")]
    public string TotalCountOfAnswerYesFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfAnswerNo { get; set; }

    [XmlElement("CountOfAnswerTotalNo")]
    public string TotalCountOfAnswerNoFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfAnswerUnspecified { get; set; }

    [XmlElement("CountOfAnswerTotalUnspecified")]
    public string TotalCountOfAnswerUnspecifiedFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int CountOfAnswerTotal { get; set; }

    [XmlElement("CountOfAnswerTotal")]
    public string CountOfAnswerTotalFormatted { get; set; } = string.Empty;

    public virtual void FormatNumbers(bool useLegacyFormat)
    {
        TotalCountOfAnswerYesFormatted = NumberFormatUtil.FormatInteger(TotalCountOfAnswerYes, useLegacyFormat);
        TotalCountOfAnswerNoFormatted = NumberFormatUtil.FormatInteger(TotalCountOfAnswerNo, useLegacyFormat);
        TotalCountOfAnswerUnspecifiedFormatted = NumberFormatUtil.FormatInteger(TotalCountOfAnswerUnspecified, useLegacyFormat);
        CountOfAnswerTotalFormatted = NumberFormatUtil.FormatInteger(CountOfAnswerTotal, useLegacyFormat);
    }
}
