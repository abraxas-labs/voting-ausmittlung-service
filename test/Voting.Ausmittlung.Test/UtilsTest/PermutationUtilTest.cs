// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Core.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class PermutationUtilTest
{
    [Fact]
    public void GeneratePermutations2C1()
    {
        var arr = new[] { 1, 0 };
        var result = PermutationUtil.GenerateUniquePermutations(arr);
        var expectedResult = new[]
        {
            new[] { 1, 0 },
            new[] { 0, 1 },
        };

        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void GeneratePermutations3C2()
    {
        var arr = new[] { 1, 1, 0 };
        var result = PermutationUtil.GenerateUniquePermutations(arr);
        var expectedResult = new[]
        {
            new[] { 1, 1, 0 },
            new[] { 1, 0, 1 },
            new[] { 0, 1, 1 },
        };

        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void GeneratePermutations5C4()
    {
        var arr = new[] { 1, 1, 1, 1, 0 };
        var result = PermutationUtil.GenerateUniquePermutations(arr);
        var expectedResult = new[]
        {
            new[] { 1, 1, 1, 1, 0 },
            new[] { 1, 1, 1, 0, 1 },
            new[] { 1, 1, 0, 1, 1 },
            new[] { 1, 0, 1, 1, 1 },
            new[] { 0, 1, 1, 1, 1 },
        };

        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void GeneratePermutations5C2()
    {
        var arr = new[] { 1, 1, 0, 0, 0 };
        var result = PermutationUtil.GenerateUniquePermutations(arr);
        var expectedResult = new[]
        {
            new[] { 1, 1, 0, 0, 0 },
            new[] { 1, 0, 1, 0, 0 },
            new[] { 1, 0, 0, 1, 0 },
            new[] { 1, 0, 0, 0, 1 },
            new[] { 0, 1, 1, 0, 0 },
            new[] { 0, 1, 0, 1, 0 },
            new[] { 0, 1, 0, 0, 1 },
            new[] { 0, 0, 1, 1, 0 },
            new[] { 0, 0, 1, 0, 1 },
            new[] { 0, 0, 0, 1, 1 },
        };

        result.Should().BeEquivalentTo(expectedResult);
    }
}
