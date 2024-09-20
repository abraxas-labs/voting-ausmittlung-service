// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC.Converter;

public class WabstiCUpperSnakeCaseConverterTest
{
    private static readonly WabstiCUpperSnakeCaseConverter Converter = new WabstiCUpperSnakeCaseConverter();

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Test", "TEST")]
    [InlineData("Sg", "SG")]
    [InlineData("KantonSg", "KANTON_SG")]
    public void Test(string? name, string expected)
    {
        Converter.ConvertToString(name, null!, null!)
            .Should()
            .Be(expected);
    }
}
