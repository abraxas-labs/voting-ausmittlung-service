// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public interface IValidator<T>
{
    IEnumerable<ValidationResult> Validate(T data, ValidationContext context);
}
