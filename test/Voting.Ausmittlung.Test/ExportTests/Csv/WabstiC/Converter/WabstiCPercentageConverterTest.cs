// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC.Converter;

public class WabstiCPercentageConverterTest
{
    private static readonly WabstiCPercentageConverter Converter = new WabstiCPercentageConverter();

    [Theory]
    [InlineData(0, "0")]
    [InlineData(0.1234, "1234")]
    [InlineData(0.1, "1000")]
    [InlineData(0.0125, "125")]
    [InlineData(0.1234555, "1235")]
    [InlineData(0.12343, "1234")]
    public void Test(decimal value, string expected)
    {
        Converter.ConvertToString(value, null!, null!)
            .Should()
            .Be(expected);
    }
}
