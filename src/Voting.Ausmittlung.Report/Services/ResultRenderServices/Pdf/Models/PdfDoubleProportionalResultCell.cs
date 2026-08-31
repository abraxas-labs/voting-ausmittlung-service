// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDoubleProportionalResultCell
{
    public PdfProportionalElectionList? List { get; set; }

    public bool ProportionalElectionQuorumReached { get; set; }

    [XmlIgnore]
    public int VoteCount { get; set; }

    [XmlElement("VoteCount")]
    public string VoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public decimal VoterNumber { get; set; }

    [XmlElement("VoterNumber")]
    public string VoterNumberFormatted { get; set; } = string.Empty;

    public int SubApportionmentNumberOfMandates { get; set; }

    [XmlIgnore]
    public decimal VoteCountPercentageInElection { get; set; }

    [XmlElement("VoteCountPercentageInElection")]
    public string VoteCountPercentageInElectionFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        VoteCountFormatted = NumberFormatUtil.FormatInteger(VoteCount, useLegacyFormat);
        VoterNumberFormatted = NumberFormatUtil.FormatDecimal(VoterNumber, useLegacyFormat, 3);
        VoteCountPercentageInElectionFormatted = NumberFormatUtil.FormatDecimal(VoteCountPercentageInElection, useLegacyFormat, 2);
    }
}
