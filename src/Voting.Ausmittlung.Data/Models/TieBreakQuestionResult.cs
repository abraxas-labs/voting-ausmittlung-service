// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class TieBreakQuestionResult : BaseEntity,
    IHasSubTotals<TieBreakQuestionResultSubTotal, TieBreakQuestionResultNullableSubTotal>,
    ITieBreakQuestionResultTotal<int>
{
    public TieBreakQuestion Question { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public BallotResult BallotResult { get; set; } = null!;

    public Guid BallotResultId { get; set; }

    public TieBreakQuestionResultSubTotal EVotingSubTotal { get; set; } = new();

    public TieBreakQuestionResultNullableSubTotal ConventionalSubTotal { get; set; } = new();

    /// <summary>
    /// Gets the total count of the answer yes.
    /// </summary>
    public int TotalCountOfAnswerQ1 => EVotingSubTotal.TotalCountOfAnswerQ1 + ConventionalSubTotal.TotalCountOfAnswerQ1.GetValueOrDefault();

    /// <summary>
    /// Gets the total count of the answer no.
    /// </summary>
    public int TotalCountOfAnswerQ2 => EVotingSubTotal.TotalCountOfAnswerQ2 + ConventionalSubTotal.TotalCountOfAnswerQ2.GetValueOrDefault();

    /// <summary>
    /// Gets the total count of the answer unspecified.
    /// </summary>
    public int TotalCountOfAnswerUnspecified => EVotingSubTotal.TotalCountOfAnswerUnspecified + ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault();

    public bool HasQ1Majority => TotalCountOfAnswerQ1 > TotalCountOfAnswerQ2;

    public bool HasQ2Majority => TotalCountOfAnswerQ2 > TotalCountOfAnswerQ1;

    public decimal PercentageQ1 =>

        // total count of answers cannot be negative, checked by business rules
        TotalCountOfAnswerQ1 == 0 && TotalCountOfAnswerQ2 == 0
            ? 0
            : (decimal)TotalCountOfAnswerQ1 / (TotalCountOfAnswerQ1 + TotalCountOfAnswerQ2);

    public decimal PercentageQ2 => 1 - PercentageQ1;

    public int CountOfAnswerTotal => TotalCountOfAnswerQ1 + TotalCountOfAnswerQ2 + TotalCountOfAnswerUnspecified;
}
