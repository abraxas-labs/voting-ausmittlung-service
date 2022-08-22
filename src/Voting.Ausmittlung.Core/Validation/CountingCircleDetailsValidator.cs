// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class CountingCircleDetailsValidator : AbstractValidator<ContestCountingCircleDetails>
{
    public CountingCircleDetailsValidator(
        IValidator<VotingCardResultDetail> vcResultDetailValidator,
        IValidator<CountOfVotersInformationSubTotal> countOfVotersInformationSubTotalValidator)
    {
        RuleForEach(v => v.VotingCards)
            .SetValidator(vcResultDetailValidator);
        RuleForEach(v => v.CountOfVotersInformation.SubTotalInfo)
            .SetValidator(countOfVotersInformationSubTotalValidator);

        RuleFor(v => v.CountOfVotersInformation.TotalCountOfVoters).GreaterThanOrEqualTo(0);

        RuleFor(v => v.CountingCircleId).NotEmpty();
        RuleFor(v => v.ContestId).NotEmpty();
    }
}
