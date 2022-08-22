// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Domain;

public class VoteBallotQuestionResult
{
    /// <summary>
    /// Gets or sets the question number that this result refers to.
    /// </summary>
    public int QuestionNumber { get; set; }

    /// <summary>
    /// Gets or sets the amount of "Yes" votes that the question received.
    /// </summary>
    public int? ReceivedCountYes { get; set; }

    /// <summary>
    /// Gets or sets the amount of "No" votes that the question received.
    /// </summary>
    public int? ReceivedCountNo { get; set; }

    /// <summary>
    /// Gets or sets the amount of unspecified votes that the question received.
    /// Only applicable for variant vote ballots.
    /// </summary>
    public int? ReceivedCountUnspecified { get; set; }
}
