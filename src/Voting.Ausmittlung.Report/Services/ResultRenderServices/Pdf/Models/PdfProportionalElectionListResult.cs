// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionListResult
{
    [XmlElement("ProportionalElectionList")]
    public PdfProportionalElectionList? List { get; set; }

    [XmlElement("VoteSource")]
    public List<PdfProportionalElectionListVoteSourceResult>? VoteSources { get; set; }

    [XmlElement("ProportionalElectionCandidateResult")]
    public List<PdfProportionalElectionCandidateResult>? CandidateResults { get; set; }

    public PdfProportionalElectionListResultSubTotal? ConventionalSubTotal { get; set; }

    public PdfProportionalElectionListResultSubTotal? EVotingSubTotal { get; set; }

    /// <summary>
    /// Gets or sets the count of unmodified lists that were handed in for this list.
    /// </summary>
    [XmlIgnore]
    public int UnmodifiedListsCount { get; set; }

    [XmlElement("UnmodifiedListsCount")]
    public string UnmodifiedListsCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of candidate votes gained from unmodified lists.
    /// </summary>
    [XmlIgnore]
    public int UnmodifiedListVotesCount { get; set; }

    [XmlElement("UnmodifiedListVotesCount")]
    public string UnmodifiedListVotesCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from unmodified lists.
    /// </summary>
    [XmlIgnore]
    public int UnmodifiedListBlankRowsCount { get; set; }

    [XmlElement("UnmodifiedListBlankRowsCount")]
    public string UnmodifiedListBlankRowsCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of modified lists that were handed in for this list.
    /// </summary>
    [XmlIgnore]
    public int ModifiedListsCount { get; set; }

    [XmlElement("ModifiedListsCount")]
    public string ModifiedListsCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of candidate votes gained from modified lists.
    /// </summary>
    [XmlIgnore]
    public int ModifiedListVotesCount { get; set; }

    [XmlElement("ModifiedListVotesCount")]
    public string ModifiedListVotesCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from modified lists.
    /// </summary>
    [XmlIgnore]
    public int ModifiedListBlankRowsCount { get; set; }

    [XmlElement("ModifiedListBlankRowsCount")]
    public string ModifiedListBlankRowsCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of votes gained from unmodified lists and from blank rows on unmodified lists.
    /// </summary>
    [XmlIgnore]
    public int UnmodifiedListVotesCountInclBlankRows { get; set; }

    [XmlElement("UnmodifiedListVotesCountInclBlankRows")]
    public string UnmodifiedListVotesCountInclBlankRowsFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of votes gained from modified lists and from blank rows on modified lists.
    /// </summary>
    [XmlIgnore]
    public int ModifiedListVotesCountInclBlankRows { get; set; }

    [XmlElement("ModifiedListVotesCountInclBlankRows")]
    public string ModifiedListVotesCountInclBlankRowsFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of candidate votes gained from unmodifed and modified lists.
    /// </summary>
    [XmlIgnore]
    public int ListVotesCount { get; set; }

    [XmlElement("ListVotesCount")]
    public string ListVotesCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of lists (modified + unmodified).
    /// </summary>
    [XmlIgnore]
    public int ListCount { get; set; }

    [XmlElement("ListCount")]
    public string ListCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from unmodified and modified lists.
    /// </summary>
    [XmlIgnore]
    public int BlankRowsCount { get; set; }

    [XmlElement("BlankRowsCount")]
    public string BlankRowsCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sum list votes count and blank rows count.
    /// </summary>
    [XmlIgnore]
    public int TotalVoteCount { get; set; }

    [XmlElement("TotalVoteCount")]
    public string TotalVoteCountFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        UnmodifiedListsCountFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListsCount, useLegacyFormat);
        ModifiedListsCountFormatted = NumberFormatUtil.FormatInteger(ModifiedListsCount, useLegacyFormat);
        ListCountFormatted = NumberFormatUtil.FormatInteger(ListCount, useLegacyFormat);
        UnmodifiedListVotesCountFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListVotesCount, useLegacyFormat);
        ModifiedListVotesCountFormatted = NumberFormatUtil.FormatInteger(ModifiedListVotesCount, useLegacyFormat);
        UnmodifiedListBlankRowsCountFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListBlankRowsCount, useLegacyFormat);
        ModifiedListBlankRowsCountFormatted = NumberFormatUtil.FormatInteger(ModifiedListBlankRowsCount, useLegacyFormat);
        UnmodifiedListVotesCountInclBlankRowsFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListVotesCountInclBlankRows, useLegacyFormat);
        ModifiedListVotesCountInclBlankRowsFormatted = NumberFormatUtil.FormatInteger(ModifiedListVotesCountInclBlankRows, useLegacyFormat);
        ListVotesCountFormatted = NumberFormatUtil.FormatInteger(ListVotesCount, useLegacyFormat);
        BlankRowsCountFormatted = NumberFormatUtil.FormatInteger(BlankRowsCount, useLegacyFormat);
        TotalVoteCountFormatted = NumberFormatUtil.FormatInteger(TotalVoteCount, useLegacyFormat);

        foreach (var voteSource in VoteSources ?? [])
        {
            voteSource.FormatNumbers(useLegacyFormat);
        }

        foreach (var candidateResult in CandidateResults ?? [])
        {
            candidateResult.FormatNumbers(useLegacyFormat);
        }

        ConventionalSubTotal?.FormatNumbers(useLegacyFormat);
        EVotingSubTotal?.FormatNumbers(useLegacyFormat);
    }
}
