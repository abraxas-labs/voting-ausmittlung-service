// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class CountingCircleDetailsValidator : AbstractValidator<ContestCountingCircleDetails>
{
    public CountingCircleDetailsValidator(
        IValidator<VotingCardResultDetail> vcResultDetailValidator)
    {
        RuleForEach(v => v.VotingCards)
            .SetValidator(vcResultDetailValidator);
    }
}
