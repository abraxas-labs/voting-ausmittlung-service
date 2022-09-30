// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Core.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class RandomUtilTest
{
    [Fact]
    public void ShouldReturnEmptyIfEmpty()
    {
        RandomUtil.Samples(Enumerable.Empty<object>(), 2).Count().Should().Be(0);
    }

    [Fact]
    public void ShouldReturnEmptyIfNoSamples()
    {
        RandomUtil.Samples(Enumerable.Range(1, 10), 0).Count().Should().Be(0);
    }

    [Fact]
    public void ShouldReturnCorrectCount()
    {
        RandomUtil.Samples(Enumerable.Range(1, 10), 2).Count().Should().Be(2);
    }

    [Fact]
    public void ShouldReturnCorrectCountIfSampleIsLargerThanCount()
    {
        RandomUtil.Samples(Enumerable.Range(3, 3), 10).Count().Should().Be(3);
    }

    [Fact]
    public void ShouldReturnRandomSamples()
    {
        var samplesGenerator = RandomUtil.Samples(Enumerable.Range(1, 100_000), 3);
        var testSamples = samplesGenerator.ToList();
        for (var i = 0; i < 3; i++)
        {
            testSamples.SequenceEqual(samplesGenerator).Should().BeFalse();
        }
    }

    [Fact]
    public void ShouldReturnCorrectSize()
    {
        RandomUtil.GetRandomString(4, "ABCDEF".ToCharArray()).Length.Should().Be(4);
    }

    [Fact]
    public void ShouldReturnOnlyWithDesiredChars()
    {
        var chars = "ABC".ToCharArray();
        var randomString = RandomUtil.GetRandomString(4, chars);
        foreach (var c in randomString)
        {
            chars.Should().Contain(c);
        }
    }

    [Fact]
    public void ShouldReturnRandomString()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        var random1 = RandomUtil.GetRandomString(6, chars);
        var random2 = RandomUtil.GetRandomString(6, chars);
        random1.Equals(random2).Should().BeFalse();
    }
}
