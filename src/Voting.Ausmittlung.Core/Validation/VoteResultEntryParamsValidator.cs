// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class VoteResultEntryParamsValidator : AbstractValidator<VoteResultEntryParams>
{
    public VoteResultEntryParamsValidator()
    {
        RuleFor(x => x.BallotBundleSampleSizePercent).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
    }
}
