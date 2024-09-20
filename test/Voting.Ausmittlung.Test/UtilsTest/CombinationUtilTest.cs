// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Core.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class CombinationUtilTest
{
    [Fact]
    public void ShouldGenerateCombinations()
    {
        var arr = new[]
        {
            new[]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
            },
            new[]
            {
                new[] { 3, 2 },
                new[] { 2, 3 },
            },
            new[]
            {
                new[] { 5, 4 },
                new[] { 4, 5 },
            },
        };
        var result = CombinationsUtil.GenerateCombinations(arr);
        var flattenedResult = result.Select(r => r.SelectMany(x => x).ToList()).ToList();
        var expectedFlattenedResult = new[]
        {
            new[] { 1, 0, 3, 2, 5, 4 },
            new[] { 1, 0, 3, 2, 4, 5 },
            new[] { 1, 0, 2, 3, 5, 4 },
            new[] { 1, 0, 2, 3, 4, 5 },
            new[] { 0, 1, 3, 2, 5, 4 },
            new[] { 0, 1, 3, 2, 4, 5 },
            new[] { 0, 1, 2, 3, 5, 4 },
            new[] { 0, 1, 2, 3, 4, 5 },
        };
        flattenedResult.Should().BeEquivalentTo(expectedFlattenedResult);
    }

    [Fact]
    public void ShouldGenerateCombinationsWithDistinctInputItems()
    {
        var arr = new[]
        {
            new[]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
            },
            new[]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
            },
        };
        var result = CombinationsUtil.GenerateCombinations(arr);
        var flattenedResult = result.Select(r => r.SelectMany(x => x).ToList()).ToList();
        var expectedFlattenedResult = new[]
        {
            new[] { 1, 0, 1, 0 },
            new[] { 1, 0, 0, 1 },
            new[] { 0, 1, 1, 0 },
            new[] { 0, 1, 0, 1 },
        };
        flattenedResult.Should().BeEquivalentTo(expectedFlattenedResult);
    }
}
