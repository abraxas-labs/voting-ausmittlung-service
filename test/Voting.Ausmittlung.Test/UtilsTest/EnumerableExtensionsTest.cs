// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.Report.Extensions;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class EnumerableExtensionsTest
{
    [Fact]
    public void TestSumNullableEmpty()
    {
        var input = Array.Empty<int?>();
        var result = input.SumNullable(x => x);
        result.Should().BeNull();
    }

    [Fact]
    public void TestSumNullableOnlyNulls()
    {
        var input = new int?[] { null, null };
        var result = input.SumNullable(x => x);
        result.Should().BeNull();
    }

    [Fact]
    public void TestSumNullableNullsAndNumbers()
    {
        var input = new int?[] { null, 1, null, 3 };
        var result = input.SumNullable(x => x);
        result.Should().Be(4);
    }

    [Fact]
    public void TestSumNullableOnlyNumbers()
    {
        var input = new int?[] { 4, 3, 2, 1 };
        var result = input.SumNullable(x => x + 1);
        result.Should().Be(14);
    }
}
