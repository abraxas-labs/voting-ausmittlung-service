// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingVoteBallotQuestionAnswer
{
    public EVotingVoteBallotQuestionAnswer(int questionNumber, BallotQuestionAnswer answer)
    {
        QuestionNumber = questionNumber;
        Answer = answer;
    }

    public int QuestionNumber { get; internal set; }

    public BallotQuestionAnswer Answer { get; internal set; }
}
