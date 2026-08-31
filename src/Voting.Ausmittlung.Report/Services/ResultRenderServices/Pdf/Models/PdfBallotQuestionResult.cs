// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBallotQuestionResult : PdfBaseQuestionResult
{
    [XmlElement("BallotQuestion")]
    public PdfBallotQuestion? Question { get; set; }

    [XmlIgnore]
    public decimal PercentageYes { get; set; }

    [XmlElement("PercentageYes")]
    public string PercentageYesFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal PercentageNo { get; set; }

    [XmlElement("PercentageNo")]
    public string PercentageNoFormatted { get; set; } = string.Empty;

    public PdfBallotQuestionResultSubTotal? EVotingSubTotal { get; set; }

    public PdfBallotQuestionResultSubTotal? ConventionalSubTotal { get; set; }

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);
        ConventionalSubTotal?.FormatNumbers(useLegacyFormat);
        EVotingSubTotal?.FormatNumbers(useLegacyFormat);
        PercentageYesFormatted = NumberFormatUtil.FormatPercentage(PercentageYes, useLegacyFormat);
        PercentageNoFormatted = NumberFormatUtil.FormatPercentage(PercentageNo, useLegacyFormat);
    }
}
