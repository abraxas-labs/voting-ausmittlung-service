// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class VoteResultValidator : CountingCircleResultValidator<VoteResult>
{
    private readonly IValidator<BallotResult> _ballotResultValidator;

    public VoteResultValidator(
        IValidator<ContestCountingCircleDetails> ccDetailsValidator,
        IValidator<BallotResult> ballotResultValidator)
        : base(ccDetailsValidator)
    {
        _ballotResultValidator = ballotResultValidator;
    }

    public override IEnumerable<ValidationResult> Validate(VoteResult data, ValidationContext context)
    {
        foreach (var result in base.Validate(data, context))
        {
            yield return result;
        }

        if (data.Results.Count > 1)
        {
            throw new ValidationException("validation for multiple ballot results is not implemented yet.");
        }

        var ballotResult = data.Results.Single();

        foreach (var result in _ballotResultValidator.Validate(ballotResult, context))
        {
            yield return result;
        }
    }
}
