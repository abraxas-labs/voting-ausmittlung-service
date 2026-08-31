// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBallotQuestionResultSubTotal : PdfBaseQuestionResult
{
    [XmlIgnore]
    public decimal PercentageYes { get; set; }

    [XmlElement("PercentageYes")]
    public string PercentageYesFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal PercentageNo { get; set; }

    [XmlElement("PercentageNo")]
    public string PercentageNoFormatted { get; set; } = string.Empty;

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);
        PercentageYesFormatted = NumberFormatUtil.FormatPercentage(PercentageYes, useLegacyFormat);
        PercentageNoFormatted = NumberFormatUtil.FormatPercentage(PercentageNo, useLegacyFormat);
    }
}
