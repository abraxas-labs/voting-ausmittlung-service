// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class TieBreakQuestionResultSubTotal : ITieBreakQuestionResultTotal<int>
{
    /// <summary>
    /// Gets or sets the total count of the answer q1.
    /// </summary>
    public int TotalCountOfAnswerQ1 { get; set; }

    /// <summary>
    /// Gets or sets the total count of the answer q2.
    /// </summary>
    public int TotalCountOfAnswerQ2 { get; set; }

    /// <summary>
    /// Gets or sets the total count of the answer unspecified.
    /// </summary>
    public int TotalCountOfAnswerUnspecified { get; set; }
}
