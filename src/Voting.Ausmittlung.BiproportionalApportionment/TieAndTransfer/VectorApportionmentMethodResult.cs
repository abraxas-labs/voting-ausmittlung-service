// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Rationals;

namespace Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

internal class VectorApportionmentMethodResult
{
    public VectorApportionmentMethodResult(int[] apportionment, Rational maxDivisor)
    {
        Apportionment = apportionment;
        MaxDivisor = maxDivisor;
    }

    public int[] Apportionment { get; }

    public Rational MaxDivisor { get; }
}
