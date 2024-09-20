// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IProportionalElectionListResultTotal
{
    /// <summary>
    /// Gets the count of unmodified lists that were handed in for this list.
    /// </summary>
    int UnmodifiedListsCount { get; }

    /// <summary>
    /// Gets the count of candidate votes gained from unmodified lists.
    /// </summary>
    int UnmodifiedListVotesCount { get; }

    /// <summary>
    /// Gets the count of votes gained from blank rows from unmodified lists.
    /// </summary>
    int UnmodifiedListBlankRowsCount { get; }

    /// <summary>
    /// Gets the count of modified lists that were handed in for this list.
    /// </summary>
    int ModifiedListsCount { get; }

    /// <summary>
    /// Gets the count of candidate votes gained from modified lists.
    /// </summary>
    int ModifiedListVotesCount { get; }

    /// <summary>
    /// Gets the count of candidate votes gained from lists assigned to another list.
    /// </summary>
    int ListVotesCountOnOtherLists { get; }

    /// <summary>
    /// Gets the count of votes gained from blank rows from modified lists.
    /// </summary>
    int ModifiedListBlankRowsCount { get; }

    /// <summary>
    /// Gets the count of votes gained from unmodified lists and from blank rows on unmodified lists.
    /// </summary>
    int UnmodifiedListVotesCountInclBlankRows { get; }

    /// <summary>
    /// Gets the count of votes gained from modified lists and from blank rows on modified lists.
    /// </summary>
    int ModifiedListVotesCountInclBlankRows { get; }

    /// <summary>
    /// Gets the count of candidate votes gained from unmodifed and modified lists.
    /// </summary>
    int ListVotesCount { get; }

    /// <summary>
    /// Gets the count of lists (modified + unmodified).
    /// </summary>
    int ListCount { get; }

    /// <summary>
    /// Gets the count of votes gained from blank rows from unmodified and modified lists.
    /// </summary>
    int BlankRowsCount { get; }

    /// <summary>
    /// Gets the sum list votes count and blank rows count.
    /// </summary>
    int TotalVoteCount { get; }
}
