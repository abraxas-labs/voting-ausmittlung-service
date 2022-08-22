// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class ElectionResultEntryParamsValidator : AbstractValidator<ElectionResultEntryParams>
{
    public ElectionResultEntryParamsValidator()
    {
        RuleFor(x => x.BallotBundleSize).GreaterThan(0);
        RuleFor(x => x.BallotBundleSampleSize).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BallotBundleSampleSize).LessThanOrEqualTo(x => x.BallotBundleSize);
    }
}
