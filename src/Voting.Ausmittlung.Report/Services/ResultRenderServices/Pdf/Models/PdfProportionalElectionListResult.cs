// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

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
    public int UnmodifiedListsCount { get; set; }

    /// <summary>
    /// Gets or sets the count of candidate votes gained from unmodified lists.
    /// </summary>
    public int UnmodifiedListVotesCount { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from unmodified lists.
    /// </summary>
    public int UnmodifiedListBlankRowsCount { get; set; }

    /// <summary>
    /// Gets or sets the count of modified lists that were handed in for this list.
    /// </summary>
    public int ModifiedListsCount { get; set; }

    /// <summary>
    /// Gets or sets the count of candidate votes gained from modified lists.
    /// </summary>
    public int ModifiedListVotesCount { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from modified lists.
    /// </summary>
    public int ModifiedListBlankRowsCount { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from unmodified lists and from blank rows on unmodified lists.
    /// </summary>
    public int UnmodifiedListVotesCountInclBlankRows { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from modified lists and from blank rows on modified lists.
    /// </summary>
    public int ModifiedListVotesCountInclBlankRows { get; set; }

    /// <summary>
    /// Gets or sets the count of candidate votes gained from unmodifed and modified lists.
    /// </summary>
    public int ListVotesCount { get; set; }

    /// <summary>
    /// Gets or sets the count of lists (modified + unmodified).
    /// </summary>
    public int ListCount { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from unmodified and modified lists.
    /// </summary>
    public int BlankRowsCount { get; set; }

    /// <summary>
    /// Gets or sets the sum list votes count and blank rows count.
    /// </summary>
    public int TotalVoteCount { get; set; }
}
