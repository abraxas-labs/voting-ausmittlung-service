// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class TieBreakQuestionDomainOfInfluenceResult :
    IHasSubTotals<TieBreakQuestionResultSubTotal>,
    ITieBreakQuestionResultTotal<int>
{
    public TieBreakQuestion Question { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public BallotResult BallotResult { get; set; } = null!;

    public Guid BallotResultId { get; set; }

    public TieBreakQuestionResultSubTotal EVotingSubTotal { get; set; } = new TieBreakQuestionResultSubTotal();

    public TieBreakQuestionResultSubTotal ECountingSubTotal { get; set; } = new TieBreakQuestionResultSubTotal();

    public TieBreakQuestionResultSubTotal ConventionalSubTotal { get; set; } = new TieBreakQuestionResultSubTotal();

    public int TotalCountOfAnswerQ1 => EVotingSubTotal.TotalCountOfAnswerQ1 + ECountingSubTotal.TotalCountOfAnswerQ1 + ConventionalSubTotal.TotalCountOfAnswerQ1;

    public int TotalCountOfAnswerQ2 => EVotingSubTotal.TotalCountOfAnswerQ2 + ECountingSubTotal.TotalCountOfAnswerQ2 + ConventionalSubTotal.TotalCountOfAnswerQ2;

    public int TotalCountOfAnswerUnspecified => EVotingSubTotal.TotalCountOfAnswerUnspecified + ECountingSubTotal.TotalCountOfAnswerUnspecified + ConventionalSubTotal.TotalCountOfAnswerUnspecified;

    public bool HasQ1Majority => TotalCountOfAnswerQ1 > TotalCountOfAnswerQ2;

    public bool HasQ2Majority => TotalCountOfAnswerQ2 > TotalCountOfAnswerQ1;

    public decimal PercentageQ1 =>

        // total count of answers cannot be negative, checked by business rules
        TotalCountOfAnswerQ1 == 0 && TotalCountOfAnswerQ2 == 0
            ? 0
            : (decimal)TotalCountOfAnswerQ1 / (TotalCountOfAnswerQ1 + TotalCountOfAnswerQ2);

    public decimal PercentageQ2 =>
        TotalCountOfAnswerQ1 == 0 && TotalCountOfAnswerQ2 == 0
            ? 0
            : 1 - PercentageQ1;

    public int CountOfAnswerTotal => TotalCountOfAnswerQ1 + TotalCountOfAnswerQ2 + TotalCountOfAnswerUnspecified;
}
