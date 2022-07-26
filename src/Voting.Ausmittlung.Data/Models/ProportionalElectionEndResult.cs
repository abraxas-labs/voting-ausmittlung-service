// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionEndResult : PoliticalBusinessEndResult,
    IHasSubTotals<ProportionalElectionResultSubTotal>,
    IProportionalElectionResultTotal
{
    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!;

    public PoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new();

    public ICollection<ProportionalElectionListEndResult> ListEndResults { get; set; } =
        new HashSet<ProportionalElectionListEndResult>();

    public ProportionalElectionResultSubTotal EVotingSubTotal { get; set; } = new();

    public ProportionalElectionResultSubTotal ConventionalSubTotal { get; set; } = new();

    /// <summary>
    /// Gets the total count of unmodified lists with a party.
    /// </summary>
    public int TotalCountOfUnmodifiedLists => EVotingSubTotal.TotalCountOfUnmodifiedLists + ConventionalSubTotal.TotalCountOfUnmodifiedLists;

    /// <summary>
    /// Gets the total count of modified lists with a party.
    /// </summary>
    public int TotalCountOfModifiedLists => EVotingSubTotal.TotalCountOfModifiedLists + ConventionalSubTotal.TotalCountOfModifiedLists;

    /// <summary>
    /// Gets the count of lists without a source list / party.
    /// </summary>
    public int TotalCountOfListsWithoutParty => EVotingSubTotal.TotalCountOfListsWithoutParty + ConventionalSubTotal.TotalCountOfListsWithoutParty;

    /// <summary>
    /// Gets the count of ballots (= total count of modified lists with and without a party).
    /// </summary>
    public int TotalCountOfBallots => TotalCountOfModifiedLists + TotalCountOfListsWithoutParty;

    /// <summary>
    /// Gets the count of votes gained from blank rows from lists/ballots without a source list / party.
    /// </summary>
    public int TotalCountOfBlankRowsOnListsWithoutParty => EVotingSubTotal.TotalCountOfBlankRowsOnListsWithoutParty + ConventionalSubTotal.TotalCountOfBlankRowsOnListsWithoutParty;

    /// <summary>
    /// Gets the total count of lists with a party (modified + unmodified).
    /// </summary>
    public int TotalCountOfListsWithParty => TotalCountOfUnmodifiedLists + TotalCountOfModifiedLists;

    /// <summary>
    /// Gets the total count of lists (without and with a party).
    /// </summary>
    public int TotalCountOfLists => TotalCountOfListsWithParty + TotalCountOfListsWithoutParty;

    public HagenbachBischoffGroup? HagenbachBischoffRootGroup { get; set; }

    public void ResetCalculation()
    {
        foreach (var listEndResult in ListEndResults)
        {
            listEndResult.NumberOfMandates = 0;
        }

        HagenbachBischoffRootGroup = null;
    }

    public void ResetAllSubTotals(VotingDataSource dataSource, bool includeCountOfVoters = false)
    {
        this.ResetSubTotal(dataSource);

        foreach (var listEndResult in ListEndResults)
        {
            listEndResult.ResetAllSubTotals(dataSource);
        }

        if (includeCountOfVoters)
        {
            CountOfVoters.ResetSubTotal(dataSource, TotalCountOfVoters);
        }
    }
}
