// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class EnterVoteBallotQuestionResultValidator : AbstractValidator<VoteBallotQuestionResult>
{
    public EnterVoteBallotQuestionResultValidator()
    {
        RuleFor(x => x.QuestionNumber).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedCountYes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedCountNo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedCountUnspecified).GreaterThanOrEqualTo(0);
    }
}
