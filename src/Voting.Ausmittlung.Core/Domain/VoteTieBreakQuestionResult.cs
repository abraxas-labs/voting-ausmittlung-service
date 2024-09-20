// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Domain;

public class VoteTieBreakQuestionResult
{
    /// <summary>
    /// Gets or sets the tie break question number that this result refers to.
    /// </summary>
    public int QuestionNumber { get; set; }

    /// <summary>
    /// Gets or sets the amount of votes that favored the question 1.
    /// </summary>
    public int? ReceivedCountQ1 { get; set; }

    /// <summary>
    /// Gets or sets the amount of votes that favored the question 2.
    /// </summary>
    public int? ReceivedCountQ2 { get; set; }

    /// <summary>
    /// Gets or sets the amount of unspecified votes that the question received.
    /// </summary>
    public int? ReceivedCountUnspecified { get; set; }
}
