// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

internal class VectorApportionmentMethodResult
{
    public VectorApportionmentMethodResult(int[] apportionment, decimal maxDivisor)
    {
        Apportionment = apportionment;
        MaxDivisor = maxDivisor;
    }

    public int[] Apportionment { get; }

    public decimal MaxDivisor { get; }
}
