// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationSummary
{
    public ValidationSummary(IReadOnlyCollection<ValidationResult> validationResults)
        : this(validationResults, string.Empty)
    {
    }

    public ValidationSummary(IReadOnlyCollection<ValidationResult> validationResults, string title)
    {
        ValidationResults = validationResults;
        Title = title;
    }

    public string Title { get; }

    public IReadOnlyCollection<ValidationResult> ValidationResults { get; }

    public bool IsValid => ValidationResults.IsValid();
}
