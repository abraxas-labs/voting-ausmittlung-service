// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// The (raw) result for a list for a specific counting circle.
/// </summary>
public class ProportionalElectionListResult : BaseEntity,
    IHasSubTotals<ProportionalElectionListResultSubTotal>,
    IProportionalElectionListResultTotal
{
    public ProportionalElectionResult Result { get; set; } = null!;

    public Guid ResultId { get; set; }

    public ProportionalElectionList List { get; set; } = null!;

    public Guid ListId { get; set; }

    public ICollection<ProportionalElectionCandidateResult> CandidateResults { get; set; } =
        new HashSet<ProportionalElectionCandidateResult>();

    public ProportionalElectionListResultSubTotal EVotingSubTotal { get; set; } = new();

    public ProportionalElectionListResultSubTotal ECountingSubTotal { get; set; } = new();

    public ProportionalElectionListResultSubTotal ConventionalSubTotal { get; set; } = new();

    /// <summary>
    /// Gets the count of unmodified lists that were handed in for this list.
    /// </summary>
    public int UnmodifiedListsCount => EVotingSubTotal.UnmodifiedListsCount + ECountingSubTotal.UnmodifiedListsCount + ConventionalSubTotal.UnmodifiedListsCount;

    /// <summary>
    /// Gets the count of candidate votes gained from unmodified lists.
    /// </summary>
    public int UnmodifiedListVotesCount => EVotingSubTotal.UnmodifiedListVotesCount + ECountingSubTotal.UnmodifiedListVotesCount + ConventionalSubTotal.UnmodifiedListVotesCount;

    /// <summary>
    /// Gets the count of votes gained from blank rows from unmodified lists.
    /// </summary>
    public int UnmodifiedListBlankRowsCount => EVotingSubTotal.UnmodifiedListBlankRowsCount + ECountingSubTotal.UnmodifiedListBlankRowsCount + ConventionalSubTotal.UnmodifiedListBlankRowsCount;

    /// <summary>
    /// Gets the count of modified lists that were handed in for this list.
    /// </summary>
    public int ModifiedListsCount => EVotingSubTotal.ModifiedListsCount + ECountingSubTotal.ModifiedListsCount + ConventionalSubTotal.ModifiedListsCount;

    /// <summary>
    /// Gets the count of candidate votes gained from modified lists.
    /// </summary>
    public int ModifiedListVotesCount => EVotingSubTotal.ModifiedListVotesCount + ECountingSubTotal.ModifiedListVotesCount + ConventionalSubTotal.ModifiedListVotesCount;

    /// <summary>
    /// Gets the count of candidate votes gained from lists assigned to another list.
    /// </summary>
    public int ListVotesCountOnOtherLists => EVotingSubTotal.ListVotesCountOnOtherLists + ECountingSubTotal.ListVotesCountOnOtherLists + ConventionalSubTotal.ListVotesCountOnOtherLists;

    /// <summary>
    /// Gets the count of votes gained from blank rows from modified lists.
    /// </summary>
    public int ModifiedListBlankRowsCount => EVotingSubTotal.ModifiedListBlankRowsCount + ECountingSubTotal.ModifiedListBlankRowsCount + ConventionalSubTotal.ModifiedListBlankRowsCount;

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

        foreach (var candidateResult in CandidateResults)
        {
            candidateResult.ResetAllSubTotals(dataSource);
        }
    }

    public void MoveECountingToConventional()
    {
        this.MoveECountingSubTotalsToConventional();

        foreach (var result in CandidateResults)
        {
            result.MoveECountingToConventional();
        }
    }
}
