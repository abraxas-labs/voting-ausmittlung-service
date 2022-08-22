// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public abstract class CountingCircleResultValidator<T> : IValidator<T>
    where T : CountingCircleResult
{
    private readonly IValidator<ContestCountingCircleDetails> _ccDetailsValidator;

    protected CountingCircleResultValidator(IValidator<ContestCountingCircleDetails> ccDetailsValidator)
    {
        _ccDetailsValidator = ccDetailsValidator;
    }

    public virtual IEnumerable<ValidationResult> Validate(T data, ValidationContext context)
    {
        var validationResults = _ccDetailsValidator.Validate(context.CurrentContestCountingCircleDetails, context);

        if (context.CurrentContestCountingCircleDetails.EVoting)
        {
            validationResults = validationResults.Append(ValidateEVoting(context.CurrentContestCountingCircleDetails));
        }

        return validationResults;
    }

    private ValidationResult ValidateEVoting(ContestCountingCircleDetails details)
    {
        return new ValidationResult(
            SharedProto.Validation.EVotingResultsImported,
            details.Contest.EVotingResultsImported);
    }
}
