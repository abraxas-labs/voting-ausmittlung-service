// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class VotingVoteBallot
{
    public VotingVoteBallot(
        IReadOnlyCollection<VotingImportVoteBallotQuestionAnswer> questionAnswers,
        IReadOnlyCollection<VotingImportVoteBallotTieBreakQuestionAnswer> tieBreakQuestionAnswers)
    {
        QuestionAnswers = questionAnswers;
        TieBreakQuestionAnswers = tieBreakQuestionAnswers;
    }

    public IReadOnlyCollection<VotingImportVoteBallotQuestionAnswer> QuestionAnswers { get; internal set; }

    public IReadOnlyCollection<VotingImportVoteBallotTieBreakQuestionAnswer> TieBreakQuestionAnswers { get; internal set; }
}
