// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionListResultSubTotal
{
    [XmlIgnore]
    public int UnmodifiedListsCount { get; set; }

    [XmlElement("UnmodifiedListsCount")]
    public string UnmodifiedListsCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int UnmodifiedListVotesCount { get; set; }

    [XmlElement("UnmodifiedListVotesCount")]
    public string UnmodifiedListVotesCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int UnmodifiedListBlankRowsCount { get; set; }

    [XmlElement("UnmodifiedListBlankRowsCount")]
    public string UnmodifiedListBlankRowsCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ModifiedListsCount { get; set; }

    [XmlElement("ModifiedListsCount")]
    public string ModifiedListsCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ModifiedListVotesCount { get; set; }

    [XmlElement("ModifiedListVotesCount")]
    public string ModifiedListVotesCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ListVotesCountOnOtherLists { get; set; }

    [XmlElement("ListVotesCountOnOtherLists")]
    public string ListVotesCountOnOtherListsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ModifiedListBlankRowsCount { get; set; }

    [XmlElement("ModifiedListBlankRowsCount")]
    public string ModifiedListBlankRowsCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int UnmodifiedListVotesCountInclBlankRows { get; set; }

    [XmlElement("UnmodifiedListVotesCountInclBlankRows")]
    public string UnmodifiedListVotesCountInclBlankRowsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ModifiedListVotesCountInclBlankRows { get; set; }

    [XmlElement("ModifiedListVotesCountInclBlankRows")]
    public string ModifiedListVotesCountInclBlankRowsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ListVotesCount { get; set; }

    [XmlElement("ListVotesCount")]
    public string ListVotesCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ListCount { get; set; }

    [XmlElement("ListCount")]
    public string ListCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int BlankRowsCount { get; set; }

    [XmlElement("BlankRowsCount")]
    public string BlankRowsCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalVoteCount { get; set; }

    [XmlElement("TotalVoteCount")]
    public string TotalVoteCountFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        UnmodifiedListsCountFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListsCount, useLegacyFormat);
        ModifiedListsCountFormatted = NumberFormatUtil.FormatInteger(ModifiedListsCount, useLegacyFormat);
        UnmodifiedListVotesCountFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListVotesCount, useLegacyFormat);
        ModifiedListVotesCountFormatted = NumberFormatUtil.FormatInteger(ModifiedListVotesCount, useLegacyFormat);
        ListVotesCountOnOtherListsFormatted = NumberFormatUtil.FormatInteger(ListVotesCountOnOtherLists, useLegacyFormat);
        UnmodifiedListBlankRowsCountFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListBlankRowsCount, useLegacyFormat);
        ModifiedListBlankRowsCountFormatted = NumberFormatUtil.FormatInteger(ModifiedListBlankRowsCount, useLegacyFormat);
        UnmodifiedListVotesCountInclBlankRowsFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListVotesCountInclBlankRows, useLegacyFormat);
        ModifiedListVotesCountInclBlankRowsFormatted = NumberFormatUtil.FormatInteger(ModifiedListVotesCountInclBlankRows, useLegacyFormat);
        ListVotesCountFormatted = NumberFormatUtil.FormatInteger(ListVotesCount, useLegacyFormat);
        ListCountFormatted = NumberFormatUtil.FormatInteger(ListCount, useLegacyFormat);
        BlankRowsCountFormatted = NumberFormatUtil.FormatInteger(BlankRowsCount, useLegacyFormat);
        TotalVoteCountFormatted = NumberFormatUtil.FormatInteger(TotalVoteCount, useLegacyFormat);
    }
}
