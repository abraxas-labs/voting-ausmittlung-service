// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionResult : ElectionResult,
    IHasSubTotals<ProportionalElectionResultSubTotal>,
    IProportionalElectionResultTotal
{
    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!;

    public ProportionalElectionResultEntryParams EntryParams { get; set; } = new();

    public ICollection<ProportionalElectionUnmodifiedListResult> UnmodifiedListResults { get; set; } =
        new HashSet<ProportionalElectionUnmodifiedListResult>();

    public ICollection<ProportionalElectionListResult> ListResults { get; set; } =
        new HashSet<ProportionalElectionListResult>();

    public ICollection<ProportionalElectionResultBundle> Bundles { get; set; } =
        new HashSet<ProportionalElectionResultBundle>();

    [NotMapped]
    public override PoliticalBusiness PoliticalBusiness => ProportionalElection;

    [NotMapped]
    public override Guid PoliticalBusinessId
    {
        get => ProportionalElectionId;
        set => ProportionalElectionId = value;
    }

    public ProportionalElectionResultSubTotal EVotingSubTotal { get; set; } = new();

    public ProportionalElectionResultSubTotal ConventionalSubTotal { get; set; } = new();

    /// <inheritdoc />
    public int TotalCountOfUnmodifiedLists => EVotingSubTotal.TotalCountOfUnmodifiedLists + ConventionalSubTotal.TotalCountOfUnmodifiedLists;

    /// <inheritdoc />
    public int TotalCountOfModifiedLists => EVotingSubTotal.TotalCountOfModifiedLists + ConventionalSubTotal.TotalCountOfModifiedLists;

    /// <inheritdoc />
    public int TotalCountOfListsWithoutParty => EVotingSubTotal.TotalCountOfListsWithoutParty + ConventionalSubTotal.TotalCountOfListsWithoutParty;

    /// <inheritdoc />
    public int TotalCountOfBallots => TotalCountOfModifiedLists + TotalCountOfListsWithoutParty;

    /// <inheritdoc />
    public int TotalCountOfBlankRowsOnListsWithoutParty => EVotingSubTotal.TotalCountOfBlankRowsOnListsWithoutParty + ConventionalSubTotal.TotalCountOfBlankRowsOnListsWithoutParty;

    /// <inheritdoc />
    public int TotalCountOfListsWithParty => TotalCountOfUnmodifiedLists + TotalCountOfModifiedLists;

    /// <inheritdoc />
    public int TotalCountOfLists => TotalCountOfListsWithParty + TotalCountOfListsWithoutParty;

    public void ResetAllSubTotals(VotingDataSource dataSource, bool includeCountOfVoters)
    {
        this.ResetSubTotal(dataSource);

        foreach (var unmodifiedListResult in UnmodifiedListResults)
        {
            unmodifiedListResult.ResetSubTotal(dataSource);
        }

        foreach (var listResult in ListResults)
        {
            listResult.ResetAllSubTotals(dataSource);
        }

        if (includeCountOfVoters)
        {
            CountOfVoters.ResetSubTotal(dataSource, TotalCountOfVoters);
        }
    }
}
