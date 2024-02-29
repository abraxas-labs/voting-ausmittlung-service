// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class BallotQuestionEndResult : BaseEntity,
    IHasSubTotals<BallotQuestionResultSubTotal>,
    IBallotQuestionResultTotal<int>
{
    public BallotQuestion Question { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public BallotEndResult BallotEndResult { get; set; } = null!;

    public Guid BallotEndResultId { get; set; }

    public BallotQuestionResultSubTotal EVotingSubTotal { get; set; } = new BallotQuestionResultSubTotal();

    public BallotQuestionResultSubTotal ConventionalSubTotal { get; set; } = new BallotQuestionResultSubTotal();

    /// <summary>
    /// Gets the total count of the answer yes.
    /// </summary>
    public int TotalCountOfAnswerYes => EVotingSubTotal.TotalCountOfAnswerYes + ConventionalSubTotal.TotalCountOfAnswerYes;

    /// <summary>
    /// Gets the total count of the answer no.
    /// </summary>
    public int TotalCountOfAnswerNo => EVotingSubTotal.TotalCountOfAnswerNo + ConventionalSubTotal.TotalCountOfAnswerNo;

    /// <summary>
    /// Gets the total count of the answer unspecified.
    /// </summary>
    public int TotalCountOfAnswerUnspecified => EVotingSubTotal.TotalCountOfAnswerUnspecified + ConventionalSubTotal.TotalCountOfAnswerUnspecified;

    // This value is updated when the state of a counting circle result changes according to to AuditedTentatively or ResettedToSubmissionFinished.
    // This value is not updated after eVoting imports (eVoting need the counting circle result state to be in correction or in submission)
    public int CountOfCountingCircleYes { get; set; }

    // This value is updated when the state of a counting circle result changes according to to AuditedTentatively or ResettedToSubmissionFinished.
    // This value is not updated after eVoting imports (eVoting need the counting circle result state to be in correction or in submission)
    public int CountOfCountingCircleNo { get; set; }

    public bool HasCountingCircleMajority { get; set; }

    public bool HasCountingCircleUnanimity { get; set; }

    public bool Accepted { get; set; }

    public decimal PercentageYes =>

        // total count of answers cannot be negative, checked by business rules
        TotalCountOfAnswerYes == 0 && TotalCountOfAnswerNo == 0
            ? 0
            : (decimal)TotalCountOfAnswerYes / (TotalCountOfAnswerYes + TotalCountOfAnswerNo);

    public decimal PercentageNo => 1 - PercentageYes;

    public int CountOfAnswerTotal => TotalCountOfAnswerYes + TotalCountOfAnswerNo + TotalCountOfAnswerUnspecified;
}
