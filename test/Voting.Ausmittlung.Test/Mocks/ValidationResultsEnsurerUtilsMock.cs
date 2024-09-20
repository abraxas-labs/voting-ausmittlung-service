// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Utils;

namespace Voting.Ausmittlung.Test.Mocks;

public class ValidationResultsEnsurerUtilsMock : IValidationResultsEnsurerUtils
{
    public void EnsureIsValid(IReadOnlyCollection<ValidationResult> validationResults)
    {
    }
}
