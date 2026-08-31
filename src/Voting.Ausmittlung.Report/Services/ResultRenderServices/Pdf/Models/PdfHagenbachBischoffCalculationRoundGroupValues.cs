// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfHagenbachBischoffCalculationRoundGroupValues
{
    [XmlElement("HagenbachBischoffGroup")]
    public PdfHagenbachBischoffSimpleGroup? Group { get; set; }

    [XmlIgnore]
    public decimal NextQuotient { get; set; }

    [XmlElement("NextQuotient")]
    public string NextQuotientFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal PreviousQuotient { get; set; }

    [XmlElement("PreviousQuotient")]
    public string PreviousQuotientFormatted { get; set; } = string.Empty;

    public int NumberOfMandates { get; set; }

    public int PreviousNumberOfMandates { get; set; }

    public int PreviousNumberOfMandatesPlusOne
    {
        get => PreviousNumberOfMandates + 1;
        set
        {
            // only needed due to xml serializer
        }
    }

    public bool IsWinner { get; set; }

    public void FormatNumbers(bool useLegacyFormat)
    {
        Group?.FormatNumbers(useLegacyFormat);

        NextQuotientFormatted = NumberFormatUtil.FormatDecimal(NextQuotient, useLegacyFormat, 3);
        PreviousQuotientFormatted = NumberFormatUtil.FormatDecimal(PreviousQuotient, useLegacyFormat, 3);
    }
}
