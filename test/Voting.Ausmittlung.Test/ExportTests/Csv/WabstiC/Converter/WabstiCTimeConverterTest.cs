// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Globalization;
using FluentAssertions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC.Converter;

public class WabstiCTimeConverterTest
{
    private static readonly WabstiCTimeConverter Converter = new WabstiCTimeConverter();

    [Theory]
    [InlineData("2008-11-01T19:35:00.0000000Z", "2035")]
    [InlineData("2008-01-12T00:00:00.0000000Z", "0100")]
    [InlineData("2008-01-12T23:00:00.0000000Z", "0000")]
    [InlineData("2008-11-01T19:35:12.1000000Z", "2035")]
    [InlineData("2008-11-01T19:35:55.1000000Z", "2035")]
    [InlineData("2008-11-01T19:35:55.1000000+0100", "1935")]
    [InlineData("2008-11-01T19:35:55.1000000+0500", "1535")]
    [InlineData(null, "")]
    public void Test(string? dateTimeStr, string expected)
    {
        var dateTime = dateTimeStr == null
            ? null
            : (DateTime?)DateTimeOffset.Parse(dateTimeStr, CultureInfo.InvariantCulture).UtcDateTime;
        Converter.ConvertToString(dateTime, null!, null!)
            .Should()
            .Be(expected);
    }
}
