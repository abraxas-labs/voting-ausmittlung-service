// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Data.Models;
using CountOfVotersInformationSubTotal = Voting.Ausmittlung.Core.Domain.CountOfVotersInformationSubTotal;

namespace Voting.Ausmittlung.Core.Validation;

public class CountOfVotersInformationSubTotalValidator : AbstractValidator<CountOfVotersInformationSubTotal>
{
    public CountOfVotersInformationSubTotalValidator()
    {
        RuleFor(x => x.Sex).IsInEnum().NotEqual(SexType.Unspecified);
        RuleFor(x => x.VoterType).IsInEnum().NotEqual(VoterType.Unspecified);
        RuleFor(x => x.CountOfVoters).GreaterThanOrEqualTo(0);
    }
}
