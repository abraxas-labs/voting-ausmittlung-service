// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Rationals;
using Voting.Ausmittlung.Core.Utils.DoubleProportional.Models;

namespace Voting.Ausmittlung.Core.Utils.DoubleProportional;

internal static class DivisorUtils
{
    private const int MaxSelectDivisorDigits = 16;

    public static Rational[] GetDivisors(DivisorApportionment[] apportionments, Rational deltaApportionment)
    {
        return apportionments
            .Select(c => new
            {
                c.Weight,
                NumberOfMandates = c.NumberOfMandates + deltaApportionment,
            })
            .Select(v => v.NumberOfMandates > 0 ? v.Weight / v.NumberOfMandates : ParseToRational(decimal.MaxValue))
            .ToArray();
    }

    public static Rational ParseToRational(decimal n)
    {
        return (Rational)n;
    }

    public static Rational ParseToRational(double n)
    {
        return (Rational)n;
    }

    public static Rational CalculateSelectDivisor(DivisorApportionment[] apportionments)
    {
        var divisorsWithOneHalfNumberOfMandateLess = GetDivisors(apportionments, new Rational(-1, 2));
        var divisorsWithOneHalfNumberOfMandateMore = GetDivisors(apportionments, new Rational(1, 2));

        var divisorUpperBoundary = divisorsWithOneHalfNumberOfMandateLess.Min();
        var divisorLowerBoundary = divisorsWithOneHalfNumberOfMandateMore.Max();

        return CalculateSelectDivisor(divisorLowerBoundary, divisorUpperBoundary);
    }

    public static Rational CalculateSelectDivisor(Rational divisorLowerBoundary, Rational divisorUpperBoundary)
    {
        var midDivisor = (divisorLowerBoundary + divisorUpperBoundary) / 2;

        for (var countOfDigitsFromLeft = 0; countOfDigitsFromLeft < MaxSelectDivisorDigits; countOfDigitsFromLeft++)
        {
            var divisor = Round(countOfDigitsFromLeft, midDivisor);
            if (divisor > divisorLowerBoundary && divisor < divisorUpperBoundary)
            {
                return divisor;
            }
        }

        return midDivisor;
    }

    private static int GetCountOfDigits(Rational n)
    {
        return (int)(1 + Math.Floor(Math.Log10((double)n)));
    }

    /// <summary>
    /// Rounds a number at the specified count of digits from the left.
    /// Ex: Number 9520 with countOfDigits 0 results in 10000.
    /// </summary>
    /// <param name="countOfDigitsFromLeft">Count of digits from the left.</param>
    /// <param name="number">The number.</param>
    /// <returns>A number which is rounded at the count of digits from the left.</returns>
    private static Rational Round(int countOfDigitsFromLeft, Rational number)
    {
        if (countOfDigitsFromLeft - GetCountOfDigits(number) < 0)
        {
            var x1 = Math.Pow(10.0, GetCountOfDigits(number) - countOfDigitsFromLeft);
            var x2 = Math.Pow(10.0, countOfDigitsFromLeft - GetCountOfDigits(number));
            return ParseToRational(x1 * Math.Round((double)number * x2));
        }
        else
        {
            return ParseToRational(Math.Round((decimal)(number * ScalePow(countOfDigitsFromLeft, number)))) / ScalePow(countOfDigitsFromLeft, number);
        }
    }

    /// <summary>
    /// Calculates the scaling multiplier.
    /// </summary>
    /// <param name="countOfDigits">Count of digits.</param>
    /// <param name="number">The number.</param>
    /// <returns>The scaling multiplier.</returns>
    private static Rational ScalePow(int countOfDigits, Rational number)
    {
        return ParseToRational(Math.Pow(10.0, countOfDigits - GetCountOfDigits(number)));
    }
}
