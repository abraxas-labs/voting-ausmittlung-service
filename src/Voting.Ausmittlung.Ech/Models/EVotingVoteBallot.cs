// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingVoteBallot
{
    public EVotingVoteBallot(
        IReadOnlyCollection<EVotingVoteBallotQuestionAnswer> questionAnswers,
        IReadOnlyCollection<EVotingVoteBallotTieBreakQuestionAnswer> tieBreakQuestionAnswers)
    {
        QuestionAnswers = questionAnswers;
        TieBreakQuestionAnswers = tieBreakQuestionAnswers;
    }

    public IReadOnlyCollection<EVotingVoteBallotQuestionAnswer> QuestionAnswers { get; internal set; }

    public IReadOnlyCollection<EVotingVoteBallotTieBreakQuestionAnswer> TieBreakQuestionAnswers { get; internal set; }
}
