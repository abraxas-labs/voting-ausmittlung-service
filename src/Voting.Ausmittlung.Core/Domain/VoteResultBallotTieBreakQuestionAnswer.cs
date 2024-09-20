// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class VoteResultBallotTieBreakQuestionAnswer
{
    /// <summary>
    /// Gets or sets the question number of the tie break question that this answer refers to.
    /// </summary>
    public int QuestionNumber { get; set; }

    public TieBreakQuestionAnswer Answer { get; set; }
}
