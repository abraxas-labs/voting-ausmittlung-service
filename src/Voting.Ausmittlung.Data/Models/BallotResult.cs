// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class BallotResult : BaseEntity
{
    public Ballot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }

    public VoteResult VoteResult { get; set; } = null!;

    public Guid VoteResultId { get; set; }

    public PoliticalBusinessNullableCountOfVoters CountOfVoters { get; set; } = new PoliticalBusinessNullableCountOfVoters();

    public ICollection<BallotQuestionResult> QuestionResults { get; set; } = new HashSet<BallotQuestionResult>();

    public ICollection<TieBreakQuestionResult> TieBreakQuestionResults { get; set; } = new HashSet<TieBreakQuestionResult>();

    public int CountOfBundlesNotReviewedOrDeleted { get; set; }

    // count of bundles not reviewed or deleted cannot be negative, since the implemented logic does not allow this
    public bool AllBundlesReviewedOrDeleted => CountOfBundlesNotReviewedOrDeleted == 0;

    public ICollection<VoteResultBundle> Bundles { get; set; } = new HashSet<VoteResultBundle>();

    /// <summary>
    /// Gets or sets total count of ballots.
    /// Only set if entry is detailed and includes only conventional ballots.
    /// Updated when a bundle changes its state to Reviewed or Deleted.
    /// </summary>
    public int ConventionalCountOfDetailedEnteredBallots { get; set; }

    public void UpdateVoterParticipation(int totalCountOfVoters) => CountOfVoters.UpdateVoterParticipation(totalCountOfVoters);

    public void OrderQuestionResultsAndSubTotals()
    {
        QuestionResults = QuestionResults
            .OrderBy(qr => qr.Question.Number)
            .ToList();

        TieBreakQuestionResults = TieBreakQuestionResults
            .OrderBy(qr => qr.Question.Number)
            .ToList();
    }

    public void ResetAllSubTotals(VotingDataSource dataSource)
    {
        var setZeroInsteadOfNull = VoteResult.Entry == VoteResultEntry.Detailed;

        foreach (var questionResult in QuestionResults)
        {
            questionResult.ResetSubTotal(dataSource, setZeroInsteadOfNull);
        }

        foreach (var tieBreakQuestionResult in TieBreakQuestionResults)
        {
            tieBreakQuestionResult.ResetSubTotal(dataSource, setZeroInsteadOfNull);
        }
    }

    public void ResetCountOfVoters(VotingDataSource dataSource, int totalCountOfVoters)
    {
        CountOfVoters.ResetSubTotal(dataSource, totalCountOfVoters);
    }
}
