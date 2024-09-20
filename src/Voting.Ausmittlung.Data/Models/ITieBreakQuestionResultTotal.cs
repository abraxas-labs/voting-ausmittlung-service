// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface ITieBreakQuestionResultTotal<out TInt>
{
    /// <summary>
    /// Gets the total count of the answer q1.
    /// </summary>
    TInt TotalCountOfAnswerQ1 { get; }

    /// <summary>
    /// Gets the total count of the answer q2.
    /// </summary>
    TInt TotalCountOfAnswerQ2 { get; }

    /// <summary>
    /// Gets the total count of the answer unspecified.
    /// </summary>
    TInt TotalCountOfAnswerUnspecified { get; }
}
