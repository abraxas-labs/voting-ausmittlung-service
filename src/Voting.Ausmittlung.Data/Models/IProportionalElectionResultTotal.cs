// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IProportionalElectionResultTotal
{
    /// <summary>
    /// Gets the total count of unmodified lists with a party.
    /// </summary>
    int TotalCountOfUnmodifiedLists { get; }

    /// <summary>
    /// Gets the total count of modified lists with a party.
    /// </summary>
    int TotalCountOfModifiedLists { get; }

    /// <summary>
    /// Gets the count of lists without a source list / party.
    /// </summary>
    int TotalCountOfListsWithoutParty { get; }

    /// <summary>
    /// Gets the count of ballots (= total count of modified lists with and without a party).
    /// </summary>
    int TotalCountOfBallots { get; }

    /// <summary>
    /// Gets the count of votes gained from blank rows from lists/ballots without a source list / party.
    /// </summary>
    int TotalCountOfBlankRowsOnListsWithoutParty { get; }

    /// <summary>
    /// Gets the total count of lists with a party (modified + unmodified).
    /// </summary>
    int TotalCountOfListsWithParty { get; }

    /// <summary>
    /// Gets the total count of lists (without and with a party).
    /// </summary>
    int TotalCountOfLists { get; }
}
