// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionListEndResult : BaseEntity,
    IHasSubTotals<ProportionalElectionListResultSubTotal>,
    IProportionalElectionListResultTotal
{
    public ProportionalElectionList List { get; set; } = null!;

    public Guid ListId { get; set; }

    public ProportionalElectionEndResult ElectionEndResult { get; set; } = null!;

    public Guid ElectionEndResultId { get; set; }

    public int NumberOfMandates { get; set; }

    public bool HasOpenRequiredLotDecisions { get; set; }

    public ICollection<ProportionalElectionCandidateEndResult> CandidateEndResults { get; set; } =
        new HashSet<ProportionalElectionCandidateEndResult>();

    public ProportionalElectionListResultSubTotal EVotingSubTotal { get; set; } = new();

    public ProportionalElectionListResultSubTotal ConventionalSubTotal { get; set; } = new();

    /// <summary>
    /// Gets the count of unmodified lists that were handed in for this list.
    /// </summary>
    public int UnmodifiedListsCount => EVotingSubTotal.UnmodifiedListsCount + ConventionalSubTotal.UnmodifiedListsCount;

    /// <summary>
    /// Gets the count of candidate votes gained from unmodified lists.
    /// </summary>
    public int UnmodifiedListVotesCount => EVotingSubTotal.UnmodifiedListVotesCount + ConventionalSubTotal.UnmodifiedListVotesCount;

    /// <summary>
    /// Gets the count of votes gained from blank rows from unmodified lists.
    /// </summary>
    public int UnmodifiedListBlankRowsCount => EVotingSubTotal.UnmodifiedListBlankRowsCount + ConventionalSubTotal.UnmodifiedListBlankRowsCount;

    /// <summary>
    /// Gets the count of modified lists that were handed in for this list.
    /// </summary>
    public int ModifiedListsCount => EVotingSubTotal.ModifiedListsCount + ConventionalSubTotal.ModifiedListsCount;

    /// <summary>
    /// Gets the count of candidate votes gained from modified lists.
    /// </summary>
    public int ModifiedListVotesCount => EVotingSubTotal.ModifiedListVotesCount + ConventionalSubTotal.ModifiedListVotesCount;

    /// <summary>
    /// Gets the count of candidate votes gained from lists assigned to another list.
    /// </summary>
    public int ListVotesCountOnOtherLists => EVotingSubTotal.ListVotesCountOnOtherLists + ConventionalSubTotal.ListVotesCountOnOtherLists;

    /// <summary>
    /// Gets the count of votes gained from blank rows from modified lists.
    /// </summary>
    public int ModifiedListBlankRowsCount => EVotingSubTotal.ModifiedListBlankRowsCount + ConventionalSubTotal.ModifiedListBlankRowsCount;

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

    public void ResetAllSubTotals(VotingDataSource dataSource)
    {
        this.ResetSubTotal(dataSource);

        foreach (var candidateEndResult in CandidateEndResults)
        {
            candidateEndResult.ResetAllSubTotals(dataSource);
        }
    }
}
