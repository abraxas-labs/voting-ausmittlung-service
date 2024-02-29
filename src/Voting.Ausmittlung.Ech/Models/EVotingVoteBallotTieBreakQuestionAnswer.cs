// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingVoteBallotTieBreakQuestionAnswer
{
    public EVotingVoteBallotTieBreakQuestionAnswer(int questionNumber, TieBreakQuestionAnswer answer)
    {
        QuestionNumber = questionNumber;
        Answer = answer;
    }

    public int QuestionNumber { get; internal set; }

    public TieBreakQuestionAnswer Answer { get; internal set; }
}
