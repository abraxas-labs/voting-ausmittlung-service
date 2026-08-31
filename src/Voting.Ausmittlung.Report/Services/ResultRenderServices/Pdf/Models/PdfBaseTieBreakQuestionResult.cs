// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBaseTieBreakQuestionResult
{
    [XmlIgnore]
    public int TotalCountOfAnswerQ1 { get; set; }

    [XmlElement("CountOfAnswerTotalQ1")]
    public string TotalCountOfAnswerQ1Formatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfAnswerQ2 { get; set; }

    [XmlElement("CountOfAnswerTotalQ2")]
    public string TotalCountOfAnswerQ2Formatted { get; set; } = string.Empty;

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
        TotalCountOfAnswerQ1Formatted = NumberFormatUtil.FormatInteger(TotalCountOfAnswerQ1, useLegacyFormat);
        TotalCountOfAnswerQ2Formatted = NumberFormatUtil.FormatInteger(TotalCountOfAnswerQ2, useLegacyFormat);
        TotalCountOfAnswerUnspecifiedFormatted = NumberFormatUtil.FormatInteger(TotalCountOfAnswerUnspecified, useLegacyFormat);
        CountOfAnswerTotalFormatted = NumberFormatUtil.FormatInteger(CountOfAnswerTotal, useLegacyFormat);
    }
}
