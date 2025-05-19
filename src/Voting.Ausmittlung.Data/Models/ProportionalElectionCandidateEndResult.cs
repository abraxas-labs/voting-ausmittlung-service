// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionCandidateEndResult : ElectionCandidateEndResult,
    IHasSubTotals<ProportionalElectionCandidateResultSubTotal>,
    IProportionalElectionCandidateResultTotal
{
    public ProportionalElectionCandidate Candidate { get; set; } = null!;

    public ProportionalElectionListEndResult ListEndResult { get; set; } = null!;

    public Guid ListEndResultId { get; set; }

    public ProportionalElectionCandidateEndResultState State { get; set; }

    public ProportionalElectionCandidateResultSubTotal ConventionalSubTotal { get; set; } = new();

    public ProportionalElectionCandidateResultSubTotal EVotingSubTotal { get; set; } = new();

    public ProportionalElectionCandidateResultSubTotal ECountingSubTotal { get; set; } = new();

    /// <inheritdoc cref="IProportionalElectionCandidateResultTotal.VoteCount"/>
    public override int VoteCount
    {
        get => UnmodifiedListVotesCount + ModifiedListVotesCount;
        set
        {
            // empty setter to store the value in the database...
        }
    }

    /// <inheritdoc />
    public int UnmodifiedListVotesCount => ConventionalSubTotal.UnmodifiedListVotesCount + EVotingSubTotal.UnmodifiedListVotesCount + ECountingSubTotal.UnmodifiedListVotesCount;

    /// <inheritdoc />
    public int ModifiedListVotesCount => ConventionalSubTotal.ModifiedListVotesCount + EVotingSubTotal.ModifiedListVotesCount + ECountingSubTotal.ModifiedListVotesCount;

    /// <inheritdoc />
    public int CountOfVotesOnOtherLists => ConventionalSubTotal.CountOfVotesOnOtherLists + EVotingSubTotal.CountOfVotesOnOtherLists + ECountingSubTotal.CountOfVotesOnOtherLists;

    /// <inheritdoc />
    public int CountOfVotesFromAccumulations => ConventionalSubTotal.CountOfVotesFromAccumulations + EVotingSubTotal.CountOfVotesFromAccumulations + ECountingSubTotal.CountOfVotesFromAccumulations;

    /// <summary>
    /// Gets or sets sources of votes for this candidate (only modified list votes are counted!).
    /// Ex. the candidate is on the fdp list and gains a vote from a svp list (by panaschieren).
    /// Then an entry in VoteSources for the vote source svp would have a count of 1.
    /// If the candidate gained votes from ballots in a bundle without a party,
    /// there is an entry with the list set to null.
    /// </summary>
    public ICollection<ProportionalElectionCandidateVoteSourceEndResult> VoteSources { get; set; }
        = new HashSet<ProportionalElectionCandidateVoteSourceEndResult>();

    public void ResetAllSubTotals(VotingDataSource dataSource)
    {
        this.ResetSubTotal(dataSource);

        foreach (var voteSource in VoteSources)
        {
            voteSource.ResetSubTotal(dataSource);
        }
    }

    public void MoveECountingToConventional()
    {
        this.MoveECountingSubTotalsToConventional();

        foreach (var voteSource in VoteSources)
        {
            voteSource.MoveECountingToConventional();
        }
    }
}
