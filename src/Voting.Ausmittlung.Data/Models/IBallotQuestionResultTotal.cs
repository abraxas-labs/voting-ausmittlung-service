// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IBallotQuestionResultTotal<out TInt>
{
    /// <summary>
    /// Gets the total count of the answer yes.
    /// </summary>
    TInt TotalCountOfAnswerYes { get; }

    /// <summary>
    /// Gets the total count of the answer no.
    /// </summary>
    TInt TotalCountOfAnswerNo { get; }

    /// <summary>
    /// Gets the total count of the answer unspecified.
    /// </summary>
    TInt TotalCountOfAnswerUnspecified { get; }
}
