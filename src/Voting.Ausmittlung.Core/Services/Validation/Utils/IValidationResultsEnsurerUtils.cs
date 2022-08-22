// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;

namespace Voting.Ausmittlung.Core.Services.Validation.Utils;

public interface IValidationResultsEnsurerUtils
{
    /// <summary>
    /// Ensures that all validation results are either optional or valid.
    /// Throws a nice exception message if there are invalid results.
    /// </summary>
    /// <param name="validationResults">The validation results.</param>
    /// <exception cref="FluentValidation.ValidationException">Thrown if there are non-optional invalid validation results.</exception>
    void EnsureIsValid(List<ValidationResult> validationResults);
}
