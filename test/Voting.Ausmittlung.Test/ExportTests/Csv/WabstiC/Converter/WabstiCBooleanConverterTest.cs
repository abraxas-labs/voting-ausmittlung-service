// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC.Converter;

public class WabstiCBooleanConverterTest
{
    private static readonly WabstiCBooleanConverter Converter = new WabstiCBooleanConverter();

    [Theory]
    [InlineData(true, "1")]
    [InlineData(false, "0")]
    [InlineData(null, "")]
    public void Test(bool? value, string expected)
    {
        Converter.ConvertToString(value, null!, null!)
            .Should()
            .Be(expected);
    }
}
