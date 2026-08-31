// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDoubleProportionalResult
{
    public int NumberOfMandates { get; set; }

    [XmlIgnore]
    public int VoteCount { get; set; }

    [XmlElement("VoteCount")]
    public string VoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int CantonalQuorum { get; set; }

    [XmlElement("CantonalQuorum")]
    public string CantonalQuorumFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal VoterNumber { get; set; }

    [XmlElement("VoterNumber")]
    public string VoterNumberFormatted { get; set; } = string.Empty;

    public decimal ElectionKey { get; set; }

    public int SuperApportionmentNumberOfMandates { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    [XmlElement("Row")]
    public List<PdfDoubleProportionalResultRow> Rows { get; set; } = new();

    [XmlElement("Column")]
    public List<PdfDoubleProportionalResultColumn> Columns { get; set; } = new();

    public void FormatNumbers(bool useLegacyFormat)
    {
        VoteCountFormatted = NumberFormatUtil.FormatInteger(VoteCount, useLegacyFormat);
        CantonalQuorumFormatted = NumberFormatUtil.FormatInteger(CantonalQuorum, useLegacyFormat);
        VoterNumberFormatted = NumberFormatUtil.FormatDecimal(VoterNumber, useLegacyFormat, 3);

        foreach (var row in Rows)
        {
            row.FormatNumbers(useLegacyFormat);
        }

        foreach (var column in Columns)
        {
            column.FormatNumbers(useLegacyFormat);
        }
    }
}
