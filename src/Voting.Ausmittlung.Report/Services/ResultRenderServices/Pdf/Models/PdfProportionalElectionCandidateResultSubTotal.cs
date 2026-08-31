// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionCandidateResultSubTotal
{
    [XmlIgnore]
    public int UnmodifiedListVotesCount { get; set; }

    [XmlElement("UnmodifiedListVotesCount")]
    public string UnmodifiedListVotesCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ModifiedListVotesCount { get; set; }

    [XmlElement("ModifiedListVotesCount")]
    public string ModifiedListVotesCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int CountOfVotesOnOtherLists { get; set; }

    [XmlElement("CountOfVotesOnOtherLists")]
    public string CountOfVotesOnOtherListsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int CountOfVotesFromAccumulations { get; set; }

    [XmlElement("CountOfVotesFromAccumulations")]
    public string CountOfVotesFromAccumulationsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int VoteCount { get; set; }

    [XmlElement("TotalCountOfVotes")]
    public string VoteCountFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        UnmodifiedListVotesCountFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListVotesCount, useLegacyFormat);
        ModifiedListVotesCountFormatted = NumberFormatUtil.FormatInteger(ModifiedListVotesCount, useLegacyFormat);
        CountOfVotesOnOtherListsFormatted = NumberFormatUtil.FormatInteger(CountOfVotesOnOtherLists, useLegacyFormat);
        CountOfVotesFromAccumulationsFormatted = NumberFormatUtil.FormatInteger(CountOfVotesFromAccumulations, useLegacyFormat);
        VoteCountFormatted = NumberFormatUtil.FormatInteger(VoteCount, useLegacyFormat);
    }
}
