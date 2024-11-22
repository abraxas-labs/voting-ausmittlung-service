// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional;

public class DivisorUtilsTest
{
    [Theory]
    [InlineData("855.0555555555555555555555556", "15391", "18")]
    [InlineData("855.055555555555555555555556", "106881944444444444444452140", "125000000000000000000009")]
    [InlineData("400.00000000000000000000000001", "40000000000000000000000000001", "100000000000000000000000000")]
    public void TestParseToRational(string decimalString, string numerator, string denominator)
    {
        var n = Convert.ToDecimal(decimalString);
        var result = DivisorUtils.ParseToRational(n);
        result.CanonicalForm.Numerator.ToString().Should().Be(numerator);
        result.CanonicalForm.Denominator.ToString().Should().Be(denominator);
    }
}
