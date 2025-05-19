// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class BallotEndResult : BaseEntity
{
    public Ballot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }

    public VoteEndResult VoteEndResult { get; set; } = null!;

    public Guid VoteEndResultId { get; set; }

    /// <summary>
    /// Gets or sets the count of voters, meaning the persons who actually voted in this ballot.
    /// In German: Stimmzettel.
    /// </summary>
    public PoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new PoliticalBusinessCountOfVoters();

    public ICollection<BallotQuestionEndResult> QuestionEndResults { get; set; } = new HashSet<BallotQuestionEndResult>();

    public ICollection<TieBreakQuestionEndResult> TieBreakQuestionEndResults { get; set; } = new HashSet<TieBreakQuestionEndResult>();

    public void ResetAllSubTotals(VotingDataSource dataSource)
    {
        foreach (var questionEndResult in QuestionEndResults)
        {
            questionEndResult.ResetSubTotal(dataSource);
        }

        foreach (var tieBreakQuestionEndResult in TieBreakQuestionEndResults)
        {
            tieBreakQuestionEndResult.ResetSubTotal(dataSource);
        }
    }

    public void ResetCountOfVoters(VotingDataSource dataSource, int totalCountOfVoters)
    {
        CountOfVoters.ResetSubTotal(dataSource, totalCountOfVoters);
    }

    public void OrderQuestionResults()
    {
        QuestionEndResults = QuestionEndResults.OrderBy(x => x.Question.Number).ToList();
        TieBreakQuestionEndResults = TieBreakQuestionEndResults.OrderBy(x => x.Question.Number).ToList();
    }

    public void MoveECountingToConventional()
    {
        CountOfVoters.MoveECountingSubTotalsToConventional();

        foreach (var result in QuestionEndResults)
        {
            result.MoveECountingSubTotalsToConventional();
        }

        foreach (var result in TieBreakQuestionEndResults)
        {
            result.MoveECountingSubTotalsToConventional();
        }
    }
}
