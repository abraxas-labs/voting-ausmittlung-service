// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class EnterVoteBallotResultsValidator : AbstractValidator<VoteBallotResults>
{
    public EnterVoteBallotResultsValidator()
    {
        RuleFor(r => r.QuestionResults)
            .NotNull()
            .Must(x => x.Count > 0);
    }
}
