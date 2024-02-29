// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionResultSubTotal : IProportionalElectionResultTotal
{
    /// <summary>
    /// Gets or sets the total count of unmodified lists with a party.
    /// </summary>
    public int TotalCountOfUnmodifiedLists { get; set; }

    /// <summary>
    /// Gets or sets the total count of modified lists with a party.
    /// </summary>
    public int TotalCountOfModifiedLists { get; set; }

    /// <summary>
    /// Gets or sets the count of lists without a source list / party.
    /// </summary>
    public int TotalCountOfListsWithoutParty { get; set; }

    /// <summary>
    /// Gets the count of ballots (= total count of modified lists with and without a party).
    /// </summary>
    public int TotalCountOfBallots => TotalCountOfModifiedLists + TotalCountOfListsWithoutParty;

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from lists/ballots without a source list / party.
    /// </summary>
    public int TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

    /// <summary>
    /// Gets the total count of lists with a party (modified + unmodified).
    /// </summary>
    public int TotalCountOfListsWithParty => TotalCountOfUnmodifiedLists + TotalCountOfModifiedLists;

    /// <summary>
    /// Gets the total count of lists (without and with a party).
    /// </summary>
    public int TotalCountOfLists => TotalCountOfListsWithParty + TotalCountOfListsWithoutParty;
}
