// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class EnterVoteBallotResultsCountOfVotersValidator : AbstractValidator<VoteBallotResultsCountOfVoters>
{
    public EnterVoteBallotResultsCountOfVotersValidator(IValidator<PoliticalBusinessCountOfVoters> countOfVotersValidator)
    {
        RuleFor(r => r.BallotId)
            .NotEmpty();

        RuleFor(r => r.CountOfVoters)
            .NotNull()
            .SetValidator(countOfVotersValidator);
    }
}
