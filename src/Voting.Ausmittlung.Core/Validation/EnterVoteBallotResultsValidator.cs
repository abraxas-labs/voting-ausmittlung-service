// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class EnterVoteBallotResultsValidator : AbstractValidator<VoteBallotResults>
{
    public EnterVoteBallotResultsValidator(
        IValidator<VoteBallotQuestionResult> questionResultValidator,
        IValidator<VoteTieBreakQuestionResult> tieBreakQuestionResultValidator,
        IValidator<PoliticalBusinessCountOfVoters> countOfVotersValidator)
    {
        RuleFor(r => r.BallotId)
            .NotEmpty();

        RuleFor(r => r.CountOfVoters)
            .NotNull()
            .SetValidator(countOfVotersValidator);

        RuleFor(r => r.QuestionResults)
            .NotNull()
            .Must(x => x.Count > 0);
        RuleForEach(r => r.QuestionResults)
            .SetValidator(questionResultValidator);
        RuleForEach(r => r.TieBreakQuestionResults)
            .SetValidator(tieBreakQuestionResultValidator);
    }
}
