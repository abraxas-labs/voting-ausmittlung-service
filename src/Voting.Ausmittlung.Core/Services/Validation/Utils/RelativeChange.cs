// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Services.Validation.Utils;

public static class RelativeChange
{
    /// <summary>
    /// Calculates the percentage that a value has been changed.
    /// The percentage is always reported as a positive value.
    /// <example>
    /// <code>
    /// CalculatePercent(200, 240); // results in 20
    /// CalculatePercent(200, 100); // results in 50
    /// CalculatePercent(-8, 20); // results in 0
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="referenceValue">The reference/original value. If this is negative or zero, the result will be zero.</param>
    /// <param name="comparisonValue">The value to compare to the reference value.</param>
    /// <returns>The percentage as a positive value. Note that 20% will be returned as 20, not as 0.2.</returns>
    public static decimal CalculatePercent(decimal referenceValue, decimal comparisonValue)
    {
        return referenceValue > 0
            ? (Math.Abs(comparisonValue - referenceValue) / referenceValue) * 100
            : 0;
    }
}
