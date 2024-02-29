// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class ElectionResultEntryParamsValidator : AbstractValidator<ElectionResultEntryParams>
{
    public ElectionResultEntryParamsValidator()
    {
        RuleFor(x => x.BallotBundleSampleSize).LessThanOrEqualTo(x => x.BallotBundleSize);
    }
}
