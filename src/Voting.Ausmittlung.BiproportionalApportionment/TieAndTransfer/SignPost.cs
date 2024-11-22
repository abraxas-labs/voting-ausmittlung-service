// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Rationals;

namespace Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

/// <summary>
/// The signpost function is associated with the rounding function. It maps a rational number to an integer and vica verca.
/// </summary>
public static class SignPost
{
    public static int Round(Rational q)
    {
        if (q <= 0)
        {
            return 0;
        }

        var x = (int)q.WholePart;
        return q >= x ? x + 1 : x;
    }

    public static Rational Get(int x)
    {
        if (x < 0)
        {
            return 0;
        }

        return new Rational(x) + new Rational(1, 2);
    }
}
