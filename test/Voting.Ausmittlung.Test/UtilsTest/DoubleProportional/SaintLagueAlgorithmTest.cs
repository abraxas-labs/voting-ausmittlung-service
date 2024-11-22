// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Rationals;
using Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Core.Utils.DoubleProportional.Models;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional;

public class SaintLagueAlgorithmTest
{
    private const int ApproxEqualsPrecision = 3;

    private readonly SaintLagueAlgorithm _algo = new();

    [Fact]
    public void TestZhKantonratswah2015()
    {
        var result = _algo.Calculate(
            new Rational[] { 86036, 56375, 49655, 20687, 21887, 13981, 12242, 7497, 7629, 8533 },
            180);

        AssertResult(
            result,
            1580,
            new[] { 54, 36, 31, 13, 14, 9, 8, 5, 5, 5 },
            new[] { 54.453M, 35.680M, 31.427M, 13.093M, 13.852M, 8.849M, 7.748M, 4.745M, 4.828M, 5.401M },
            0);
    }

    [Fact]
    public void ShouldWorkWithInitiallyTooManyDistributedSeats()
    {
        var result = _algo.Calculate(new Rational[] { 3600, 600, 800 }, 5);

        AssertResult(
            result,
            1100,
            new[] { 3, 1, 1 },
            new[] { 3.273M, 0.545M, 0.727M },
            0);
    }

    [Fact]
    public void ShouldWorkWithInitiallyTooManyDistributedSeatsAndLotDecisions()
    {
        var result = _algo.Calculate(new Rational[] { 3500, 500, 1000 }, 5);

        AssertResult(
            result,
            1000,
            new[] { 4, 1, 1 },
            new[] { 3.5M, 0.5M, 1M },
            1,
            new[] { TieState.Negative, TieState.Negative, TieState.Unique });
    }

    [Fact]
    public void ShouldWorkWithInitiallyTooFewDistributedSeatsAndLotDecisions()
    {
        var result = _algo.Calculate(new Rational[] { 450, 450, 100, 4000 }, 5);

        AssertResult(
            result,
            900,
            new[] { 1, 1, 0, 4 },
            new[] { 0.5M, 0.5M, 0.111M, 4.444M },
            1,
            new[] { TieState.Negative, TieState.Negative, TieState.Unique, TieState.Unique });
    }

    [Fact]
    public void ShouldWorkWithInitiallyTooFewDistributedSeats()
    {
        var result = _algo.Calculate(new Rational[] { 450, 400, 150, 4000 }, 5);

        AssertResult(
            result,
            890,
            new[] { 1, 0, 0, 4 },
            new[] { 0.506M, 0.449M, 0.169M, 4.494M },
            0);
    }

    private void AssertResult(
        SaintLagueAlgorithmResult? result,
        decimal expectedElectionKey,
        int[] expectedApportionment,
        decimal[] expectedQuotients,
        int expectedCountOfMissingNumberOfMandates,
        TieState[]? expectedTieStates = null)
    {
        result.Should().NotBeNull();
        ((decimal)result!.ElectionKey).Should().BeApproximately(expectedElectionKey, ApproxEqualsPrecision);
        result.Apportionment.SequenceEqual(expectedApportionment).Should().BeTrue();
        result.Quotients.Select(q => (decimal)q).SequenceApproxEqual(expectedQuotients, ApproxEqualsPrecision).Should().BeTrue();
        result.CountOfMissingNumberOfMandates.Should().Be(expectedCountOfMissingNumberOfMandates);

        expectedTieStates ??= Enumerable.Repeat(TieState.Unique, result.Apportionment.Length).ToArray();
        result.TieStates.SequenceEqual(expectedTieStates).Should().BeTrue();
    }
}
