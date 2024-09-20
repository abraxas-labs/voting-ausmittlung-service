// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class BallotQuestionResultNullableSubTotal : IBallotQuestionResultTotal<int?>, INullableSubTotal<BallotQuestionResultSubTotal>
{
    /// <summary>
    /// Gets or sets the total count of the answer yes.
    /// </summary>
    public int? TotalCountOfAnswerYes { get; set; }

    /// <summary>
    /// Gets or sets the total count of the answer no.
    /// </summary>
    public int? TotalCountOfAnswerNo { get; set; }

    /// <summary>
    /// Gets or sets the total count of the answer unspecified.
    /// </summary>
    public int? TotalCountOfAnswerUnspecified { get; set; }

    public int CountOfAnswerTotal => TotalCountOfAnswerYes.GetValueOrDefault() + TotalCountOfAnswerNo.GetValueOrDefault() + TotalCountOfAnswerUnspecified.GetValueOrDefault();

    public BallotQuestionResultSubTotal MapToNonNullableSubTotal()
    {
        return new BallotQuestionResultSubTotal
        {
            TotalCountOfAnswerYes = TotalCountOfAnswerYes.GetValueOrDefault(),
            TotalCountOfAnswerNo = TotalCountOfAnswerNo.GetValueOrDefault(),
            TotalCountOfAnswerUnspecified = TotalCountOfAnswerUnspecified.GetValueOrDefault(),
        };
    }

    public void ReplaceNullValuesWithZero()
    {
        TotalCountOfAnswerYes ??= 0;
        TotalCountOfAnswerNo ??= 0;
        TotalCountOfAnswerUnspecified ??= 0;
    }
}
