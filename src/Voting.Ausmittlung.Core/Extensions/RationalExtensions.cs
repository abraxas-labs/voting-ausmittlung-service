// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Numerics;
using Rationals;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Extensions;

public static class RationalExtensions
{
    public static SelectDivisorCalculation ToSelectDivisorCalculation(this Rational rational)
    {
        return new SelectDivisorCalculation
        {
            SelectDivisor = rational.ToRationalNumber(),
            LowerBoundary = rational.ToRationalNumber(),
            UpperBoundary = rational.ToRationalNumber(),
        };
    }

    public static RationalNumber ToRationalNumber(this Rational rational)
    {
        return new()
        {
            Dividend = rational.Numerator.ToString(),
            Divisor = rational.Denominator.ToString(),
            Quotient = (decimal)rational,
        };
    }

    public static Rational ToRational(this RationalNumber rationalNumber)
    {
        if (!BigInteger.TryParse(rationalNumber.Dividend, out var dividend))
        {
            throw new InvalidOperationException($"Cannot parse dividend to big integer: {rationalNumber.Dividend}");
        }

        if (!BigInteger.TryParse(rationalNumber.Divisor, out var divisor))
        {
            throw new InvalidOperationException($"Cannot parse divisor to big integer: {rationalNumber.Divisor}");
        }

        return new Rational(dividend, divisor);
    }
}
