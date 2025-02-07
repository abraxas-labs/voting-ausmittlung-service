// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Rationals;

namespace Voting.Ausmittlung.BiproportionalApportionment.Extensions;

public static class RationalExtensions
{
    private static readonly Rational _half = new Rational(1, 2);

    /// <summary>
    /// Commercial rounds a rational to an integer.
    /// </summary>
    /// <param name="rational">Rational number.</param>
    /// <returns>Rounded integer.</returns>
    public static int Round(this Rational rational)
    {
        var integer = (int)rational.WholePart;
        var fractional = rational.FractionPart.CanonicalForm;

        return fractional < _half
            ? integer
            : integer + 1;
    }
}
