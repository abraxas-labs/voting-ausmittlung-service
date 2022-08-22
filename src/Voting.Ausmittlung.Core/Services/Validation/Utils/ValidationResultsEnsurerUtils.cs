// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Ausmittlung.Core.Services.Validation.Models;

namespace Voting.Ausmittlung.Core.Services.Validation.Utils;

public class ValidationResultsEnsurerUtils : IValidationResultsEnsurerUtils
{
    /// <inheritdoc />
    public void EnsureIsValid(List<ValidationResult> validationResults)
    {
        if (validationResults.IsValid())
        {
            return;
        }

        var failedValidations = validationResults
            .Where(r => !r.IsValid && !r.IsOptional)
            .Select(r => r.Validation);

        throw new ValidationException($"Validation failed for {string.Join(",", failedValidations)}");
    }
}
