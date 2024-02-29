// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC.Converter;

public class WabstiCEndResultFinalizedConverterTest
{
    private static readonly WabstiCEndResultFinalizedConverter Converter = new WabstiCEndResultFinalizedConverter();

    [Theory]
    [InlineData(true, "2")]
    [InlineData(false, "1")]
    [InlineData(null, "1")]
    public void Test(bool? finalized, string expected)
    {
        Converter.ConvertToString(finalized, null!, null!)
            .Should()
            .Be(expected);
    }
}
