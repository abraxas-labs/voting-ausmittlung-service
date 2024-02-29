// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC.Converter;

public class WabstiCSexConverterTest
{
    private static readonly WabstiCSexConverter Converter = new WabstiCSexConverter();

    [Theory]
    [InlineData(SexType.Female, "W")]
    [InlineData(SexType.Male, "M")]
    [InlineData(SexType.Undefined, "")]
    [InlineData((SexType)100, "")]
    public void Test(SexType? value, string expected)
    {
        Converter.ConvertToString(value, null!, null!)
            .Should()
            .Be(expected);
    }
}
