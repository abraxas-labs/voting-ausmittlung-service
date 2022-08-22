// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class EnterVoteTieBreakQuestionResultValidator : AbstractValidator<VoteTieBreakQuestionResult>
{
    public EnterVoteTieBreakQuestionResultValidator()
    {
        RuleFor(x => x.QuestionNumber).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedCountQ1).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedCountQ2).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedCountUnspecified).GreaterThanOrEqualTo(0);
    }
}
