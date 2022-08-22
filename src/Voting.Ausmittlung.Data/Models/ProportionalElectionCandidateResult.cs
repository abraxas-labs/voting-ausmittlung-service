// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// The (raw) result for a candidate in a specific counting circle.
/// </summary>
public class ProportionalElectionCandidateResult : BaseEntity, IHasSubTotals<ProportionalElectionCandidateResultSubTotal>, IProportionalElectionCandidateResultTotal
{
    public ProportionalElectionListResult ListResult { get; set; } = null!;

    public Guid ListResultId { get; set; }

    public ProportionalElectionCandidate Candidate { get; set; } = null!;

    public Guid CandidateId { get; set; }

    public ProportionalElectionCandidateResultSubTotal EVotingSubTotal { get; set; } = new();

    public ProportionalElectionCandidateResultSubTotal ConventionalSubTotal { get; set; } = new();

    /// <inheritdoc />
    public int UnmodifiedListVotesCount => EVotingSubTotal.UnmodifiedListVotesCount + ConventionalSubTotal.UnmodifiedListVotesCount;

    /// <inheritdoc />
    public int ModifiedListVotesCount => EVotingSubTotal.ModifiedListVotesCount + ConventionalSubTotal.ModifiedListVotesCount;

    /// <inheritdoc />
    public int CountOfVotesOnOtherLists => EVotingSubTotal.CountOfVotesOnOtherLists + ConventionalSubTotal.CountOfVotesOnOtherLists;

    /// <inheritdoc />
    public int CountOfVotesFromAccumulations => EVotingSubTotal.CountOfVotesFromAccumulations + ConventionalSubTotal.CountOfVotesFromAccumulations;

    /// <inheritdoc />
    public int VoteCount => UnmodifiedListVotesCount + ModifiedListVotesCount;

    /// <summary>
    /// Gets or sets sources of votes for this candidate (only modified list votes are counted!).
    /// Ex. the candidate is on the fdp list and gains a vote from a svp list (by panaschieren).
    /// Then an entry in VoteSources for the vote source svp would have a count of 1.
    /// If the candidate gained votes from ballots in a bundle without a party,
    /// there is an entry with the list set to null.
    /// </summary>
    public ICollection<ProportionalElectionCandidateVoteSourceResult> VoteSources { get; set; }
        = new HashSet<ProportionalElectionCandidateVoteSourceResult>();

    public void ResetAllSubTotals(VotingDataSource dataSource)
    {
        this.ResetSubTotal(dataSource);

        foreach (var voteSource in VoteSources)
        {
            voteSource.ResetSubTotal(dataSource);
        }
    }
}
