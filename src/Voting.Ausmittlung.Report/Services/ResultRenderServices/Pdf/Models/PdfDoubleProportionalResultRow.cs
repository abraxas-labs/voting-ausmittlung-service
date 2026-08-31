// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDoubleProportionalResultRow
{
    public PdfProportionalElection? ProportionalElection { get; set; }

    [XmlIgnore]
    public int VoteCount { get; set; }

    [XmlElement("VoteCount")]
    public string VoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int VoterNumber { get; set; }

    [XmlElement("VoterNumber")]
    public string VoterNumberFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int Quorum { get; set; }

    [XmlElement("Quorum")]
    public string QuorumFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal Divisor { get; set; }

    [XmlElement("Divisor")]
    public string DivisorFormatted { get; set; } = string.Empty;

    public int NumberOfMandates { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    [XmlElement("Cell")]
    public List<PdfDoubleProportionalResultCell>? Cells { get; set; }

    public void FormatNumbers(bool useLegacyFormat)
    {
        VoteCountFormatted = NumberFormatUtil.FormatInteger(VoteCount, useLegacyFormat);
        VoterNumberFormatted = NumberFormatUtil.FormatInteger(VoterNumber, useLegacyFormat);
        QuorumFormatted = NumberFormatUtil.FormatInteger(Quorum, useLegacyFormat);
        DivisorFormatted = NumberFormatUtil.FormatDecimal(Divisor, useLegacyFormat, 3);

        foreach (var cell in Cells ?? [])
        {
            cell.FormatNumbers(useLegacyFormat);
        }
    }
}
