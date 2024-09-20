// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class VoteResultBallotQuestionAnswer
{
    public int QuestionNumber { get; set; }

    public BallotQuestionAnswer Answer { get; set; }
}
