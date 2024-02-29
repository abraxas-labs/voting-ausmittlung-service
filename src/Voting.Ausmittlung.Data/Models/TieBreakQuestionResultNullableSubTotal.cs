// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class TieBreakQuestionResultNullableSubTotal : ITieBreakQuestionResultTotal<int?>, INullableSubTotal<TieBreakQuestionResultSubTotal>
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
}
