// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionListResultSubTotal : IProportionalElectionListResultTotal, ISummableSubTotal<ProportionalElectionListResultSubTotal>
{
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
    /// Gets or sets the count of candidate votes gained from lists assigned to another list.
    /// </summary>
    public int ListVotesCountOnOtherLists { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from modified lists.
    /// </summary>
    public int ModifiedListBlankRowsCount { get; set; }

    /// <summary>
    /// Gets the count of votes gained from unmodified lists and from blank rows on unmodified lists.
    /// </summary>
    public int UnmodifiedListVotesCountInclBlankRows => UnmodifiedListVotesCount + UnmodifiedListBlankRowsCount;

    /// <summary>
    /// Gets the count of votes gained from modified lists and from blank rows on modified lists.
    /// </summary>
    public int ModifiedListVotesCountInclBlankRows => ModifiedListVotesCount + ModifiedListBlankRowsCount;

    /// <summary>
    /// Gets the count of candidate votes gained from unmodifed and modified lists.
    /// </summary>
    public int ListVotesCount => UnmodifiedListVotesCount + ModifiedListVotesCount;

    /// <summary>
    /// Gets the count of lists (modified + unmodified).
    /// </summary>
    public int ListCount => UnmodifiedListsCount + ModifiedListsCount;

    /// <summary>
    /// Gets the count of votes gained from blank rows from unmodified and modified lists.
    /// </summary>
    public int BlankRowsCount => UnmodifiedListBlankRowsCount + ModifiedListBlankRowsCount;

    /// <summary>
    /// Gets the sum list votes count and blank rows count.
    /// </summary>
    public int TotalVoteCount => ListVotesCount + BlankRowsCount;

    public void Add(ProportionalElectionListResultSubTotal other, int deltaFactor = 1)
    {
        UnmodifiedListsCount += other.UnmodifiedListsCount;
        UnmodifiedListVotesCount += other.UnmodifiedListVotesCount;
        UnmodifiedListBlankRowsCount += other.UnmodifiedListBlankRowsCount;
        ModifiedListsCount += other.ModifiedListsCount;
        ModifiedListVotesCount += other.ModifiedListVotesCount;
        ListVotesCountOnOtherLists += other.ListVotesCountOnOtherLists;
        ModifiedListBlankRowsCount += other.ModifiedListBlankRowsCount;
    }
}
