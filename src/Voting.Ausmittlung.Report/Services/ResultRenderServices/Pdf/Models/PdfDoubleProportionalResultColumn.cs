// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDoubleProportionalResultColumn
{
    public PdfProportionalElectionUnionList? UnionList { get; set; }

    public PdfProportionalElectionList? List { get; set; }

    [XmlIgnore]
    public int VoteCount { get; set; }

    [XmlElement("VoteCount")]
    public string VoteCountFormatted { get; set; } = string.Empty;

    public bool CantonalQuorumReached { get; set; }

    [XmlElement("AnyQuorumReached")]
    public bool AnyRequiredQuorumReached { get; set; }

    [XmlIgnore]
    public decimal VoterNumber { get; set; }

    [XmlElement("VoterNumber")]
    public string VoterNumberFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal SuperApportionmentQuotient { get; set; }

    [XmlElement("SuperApportionmentNumberOfMandatesUnrounded")]
    public string SuperApportionmentQuotientFormatted { get; set; } = string.Empty;

    public int SuperApportionmentNumberOfMandates { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    [XmlIgnore]
    public decimal Divisor { get; set; }

    [XmlElement("Divisor")]
    public string DivisorFormatted { get; set; } = string.Empty;

    [XmlElement("Cell")]
    public List<PdfDoubleProportionalResultCell>? Cells { get; set; } = new();

    public void FormatNumbers(bool useLegacyFormat)
    {
        VoteCountFormatted = NumberFormatUtil.FormatInteger(VoteCount, useLegacyFormat);
        VoterNumberFormatted = NumberFormatUtil.FormatDecimal(VoterNumber, useLegacyFormat, 4);
        DivisorFormatted = NumberFormatUtil.FormatDecimal(Divisor, useLegacyFormat, 3);
        SuperApportionmentQuotientFormatted = NumberFormatUtil.FormatDecimal(SuperApportionmentQuotient, useLegacyFormat, 6);

        foreach (var cell in Cells ?? [])
        {
            cell.FormatNumbers(useLegacyFormat);
        }
    }
}
