// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class SelectDivisorCalculation
{
    public RationalNumber SelectDivisor { get; set; } = new();

    public RationalNumber UpperBoundary { get; set; } = new();

    public RationalNumber LowerBoundary { get; set; } = new();
}
