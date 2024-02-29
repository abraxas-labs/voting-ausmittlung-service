// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class TieBreakQuestionEndResult : BaseEntity,
    IHasSubTotals<TieBreakQuestionResultSubTotal>,
    ITieBreakQuestionResultTotal<int>
{
    public TieBreakQuestion Question { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public BallotEndResult BallotEndResult { get; set; } = null!;

    public Guid BallotEndResultId { get; set; }

    public TieBreakQuestionResultSubTotal EVotingSubTotal { get; set; } = new();

    public TieBreakQuestionResultSubTotal ConventionalSubTotal { get; set; } = new();

    /// <summary>
    /// Gets the total count of the answer yes.
    /// </summary>
    public int TotalCountOfAnswerQ1 => EVotingSubTotal.TotalCountOfAnswerQ1 + ConventionalSubTotal.TotalCountOfAnswerQ1;

    /// <summary>
    /// Gets the total count of the answer no.
    /// </summary>
    public int TotalCountOfAnswerQ2 => EVotingSubTotal.TotalCountOfAnswerQ2 + ConventionalSubTotal.TotalCountOfAnswerQ2;

    /// <summary>
    /// Gets the total count of the answer unspecified.
    /// </summary>
    public int TotalCountOfAnswerUnspecified => EVotingSubTotal.TotalCountOfAnswerUnspecified + ConventionalSubTotal.TotalCountOfAnswerUnspecified;

    // This value is updated when the state of a counting circle result changes according to to AuditedTentatively or ResettedToSubmissionFinished.
    // This value is not updated after eVoting imports (eVoting need the counting circle result state to be in correction or in submission)
    public int CountOfCountingCircleQ1 { get; set; }

    // This value is updated when the state of a counting circle result changes according to to AuditedTentatively or ResettedToSubmissionFinished.
    // This value is not updated after eVoting imports (eVoting need the counting circle result state to be in correction or in submission)
    public int CountOfCountingCircleQ2 { get; set; }

    public bool HasCountingCircleQ1Majority { get; set; }

    public bool HasCountingCircleQ2Majority { get; set; }

    public bool Q1Accepted { get; set; }

    public decimal PercentageQ1 =>

        // total count of answers cannot be negative, checked by business rules
        TotalCountOfAnswerQ1 == 0 && TotalCountOfAnswerQ2 == 0
            ? 0
            : (decimal)TotalCountOfAnswerQ1 / (TotalCountOfAnswerQ1 + TotalCountOfAnswerQ2);

    public decimal PercentageQ2 => 1 - PercentageQ1;

    public int CountOfAnswerTotal => TotalCountOfAnswerQ1 + TotalCountOfAnswerQ2 + TotalCountOfAnswerUnspecified;
}
