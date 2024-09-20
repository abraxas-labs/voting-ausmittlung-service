// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class BallotQuestionResult : BaseEntity,
    IHasSubTotals<BallotQuestionResultSubTotal, BallotQuestionResultNullableSubTotal>,
    IBallotQuestionResultTotal<int>
{
    public BallotQuestion Question { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public BallotResult BallotResult { get; set; } = null!;

    public Guid BallotResultId { get; set; }

    public BallotQuestionResultSubTotal EVotingSubTotal { get; set; } = new BallotQuestionResultSubTotal();

    public BallotQuestionResultNullableSubTotal ConventionalSubTotal { get; set; } = new BallotQuestionResultNullableSubTotal();

    /// <summary>
    /// Gets the total count of the answer yes.
    /// </summary>
    public int TotalCountOfAnswerYes => EVotingSubTotal.TotalCountOfAnswerYes + ConventionalSubTotal.TotalCountOfAnswerYes.GetValueOrDefault();

    /// <summary>
    /// Gets the total count of the answer no.
    /// </summary>
    public int TotalCountOfAnswerNo => EVotingSubTotal.TotalCountOfAnswerNo + ConventionalSubTotal.TotalCountOfAnswerNo.GetValueOrDefault();

    /// <summary>
    /// Gets the total count of the answer unspecified.
    /// </summary>
    public int TotalCountOfAnswerUnspecified => EVotingSubTotal.TotalCountOfAnswerUnspecified + ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault();

    public bool HasMajority => TotalCountOfAnswerYes > TotalCountOfAnswerNo;

    public decimal PercentageYes =>

        // total count of answers cannot be negative, checked by business rules
        TotalCountOfAnswerYes == 0 && TotalCountOfAnswerNo == 0
            ? 0
            : (decimal)TotalCountOfAnswerYes / (TotalCountOfAnswerYes + TotalCountOfAnswerNo);

    public decimal PercentageNo => 1 - PercentageYes;

    public int CountOfAnswerTotal => TotalCountOfAnswerYes + TotalCountOfAnswerNo + TotalCountOfAnswerUnspecified;
}
