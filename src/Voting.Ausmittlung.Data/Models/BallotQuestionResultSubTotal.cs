// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class BallotQuestionResultSubTotal : IBallotQuestionResultTotal<int>, ISummableSubTotal<BallotQuestionResultSubTotal>
{
    /// <summary>
    /// Gets or sets the total count of the answer yes.
    /// </summary>
    public int TotalCountOfAnswerYes { get; set; }

    /// <summary>
    /// Gets or sets the total count of the answer no.
    /// </summary>
    public int TotalCountOfAnswerNo { get; set; }

    /// <summary>
    /// Gets or sets the total count of the answer unspecified.
    /// </summary>
    public int TotalCountOfAnswerUnspecified { get; set; }

    public void Add(BallotQuestionResultSubTotal other, int deltaFactor = 1)
    {
        TotalCountOfAnswerYes += other.TotalCountOfAnswerYes * deltaFactor;
        TotalCountOfAnswerNo += other.TotalCountOfAnswerNo * deltaFactor;
        TotalCountOfAnswerUnspecified += other.TotalCountOfAnswerUnspecified * deltaFactor;
    }
}
