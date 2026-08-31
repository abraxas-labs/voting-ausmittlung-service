// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfTieBreakQuestionResultSubTotal : PdfBaseTieBreakQuestionResult
{
    [XmlIgnore]
    public decimal PercentageQ1 { get; set; }

    [XmlElement("PercentageQ1")]
    public string PercentageQ1Formatted { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal PercentageQ2 { get; set; }

    [XmlElement("PercentageQ2")]
    public string PercentageQ2Formatted { get; set; } = string.Empty;

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);
        PercentageQ1Formatted = NumberFormatUtil.FormatPercentage(PercentageQ1, useLegacyFormat);
        PercentageQ2Formatted = NumberFormatUtil.FormatPercentage(PercentageQ2, useLegacyFormat);
    }
}
