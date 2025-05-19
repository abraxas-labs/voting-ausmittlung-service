// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class TieBreakQuestionResultNullableSubTotal : ITieBreakQuestionResultTotal<int?>, INullableSubTotal<TieBreakQuestionResultSubTotal>, ISummableSubTotal<TieBreakQuestionResultSubTotal>
{
    public int? TotalCountOfAnswerQ1 { get; set; }

    public int? TotalCountOfAnswerQ2 { get; set; }

    public int? TotalCountOfAnswerUnspecified { get; set; }

    public int CountOfAnswerTotal => TotalCountOfAnswerQ1.GetValueOrDefault() + TotalCountOfAnswerQ2.GetValueOrDefault() + TotalCountOfAnswerUnspecified.GetValueOrDefault();

    public TieBreakQuestionResultSubTotal MapToNonNullableSubTotal()
    {
        return new TieBreakQuestionResultSubTotal
        {
            TotalCountOfAnswerQ1 = TotalCountOfAnswerQ1.GetValueOrDefault(),
            TotalCountOfAnswerQ2 = TotalCountOfAnswerQ2.GetValueOrDefault(),
            TotalCountOfAnswerUnspecified = TotalCountOfAnswerUnspecified.GetValueOrDefault(),
        };
    }

    public void ReplaceNullValuesWithZero()
    {
        TotalCountOfAnswerQ1 ??= 0;
        TotalCountOfAnswerQ2 ??= 0;
        TotalCountOfAnswerUnspecified ??= 0;
    }

    public void Add(TieBreakQuestionResultSubTotal other, int deltaFactor = 1)
    {
        ReplaceNullValuesWithZero();
        TotalCountOfAnswerQ1 += other.TotalCountOfAnswerQ1 * deltaFactor;
        TotalCountOfAnswerQ2 += other.TotalCountOfAnswerQ2 * deltaFactor;
        TotalCountOfAnswerUnspecified += other.TotalCountOfAnswerUnspecified * deltaFactor;
    }
}
