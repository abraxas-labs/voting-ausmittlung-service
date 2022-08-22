// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC.Converter;

public class WabstiCIntEnumConverterTest
{
    private static readonly WabstiCIntEnumConverter Converter = new WabstiCIntEnumConverter();

    [Flags]
    public enum TestEnums
    {
        /// <summary>
        /// Test enum none.
        /// </summary>
        None = 0,

        /// <summary>
        /// Test enum value 1.
        /// </summary>
        Value1 = 1 << 0,

        /// <summary>
        /// Test enum value 2.
        /// </summary>
        Value2 = 1 << 1,

        /// <summary>
        /// Test enum value 3.
        /// </summary>
        Value3 = 1 << 2,
    }

    [Theory]
    [InlineData(TestEnums.None, "0")]
    [InlineData(TestEnums.Value1, "1")]
    [InlineData(TestEnums.Value2, "2")]
    [InlineData(TestEnums.Value2 | TestEnums.Value3, "6")]
    public void Test(TestEnums value, string expectedValue)
    {
        Converter.ConvertToString(value, null!, null!)
            .Should()
            .Be(expectedValue);
    }
}
