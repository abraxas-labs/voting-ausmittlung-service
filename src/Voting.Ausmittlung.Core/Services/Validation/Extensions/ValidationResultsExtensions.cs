// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;

namespace System.Collections.Generic;

public static class ValidationResultsExtensions
{
    /// <summary>
    /// Returns the first invalid validation result. If no invalid validation result is found, the first valid validation result is returned.
    /// </summary>
    /// <param name="results">The validation results.</param>
    /// <returns>The first invalid validation result if available, otherwise the first valid validation result.</returns>
    public static ValidationResult FirstInvalidOrElseFirstValid(this IEnumerable<ValidationResult> results)
    {
        foreach (var result in results)
        {
            if (!result.IsValid)
            {
                return result;
            }
        }

        return results.First();
    }

    /// <summary>
    /// Checks whether all validation results are either optional or valid.
    /// </summary>
    /// <param name="results">The validation results to check.</param>
    /// <returns>Whether all validation results are either optional or valid.</returns>
    public static bool IsValid(this IEnumerable<ValidationResult> results)
    {
        return results.All(x => x.IsOptional || x.IsValid);
    }
}
