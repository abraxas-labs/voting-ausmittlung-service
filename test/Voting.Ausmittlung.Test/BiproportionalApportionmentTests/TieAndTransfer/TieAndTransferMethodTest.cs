// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.BiproportionalApportionment;
using Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;
using Xunit;

namespace Voting.Ausmittlung.Test.BiproportionalApportionmentTests.TieAndTransfer;

public class TieAndTransferMethodTest
{
    private const int DecimalPrecision = 3;
    private readonly TieAndTransferApportionmentMethod _divisorApportionmentMethod = new();

    [Fact]
    public void TestKantonratswahl2019()
    {
        var t = ZhKantonratswahlTestData.Kantonratswahl2019();
        var result = _divisorApportionmentMethod.Calculate(new BiproportionalApportionmentData(t.Weights, t.ElectionNumberOfMandates, t.UnionListNumberOfMandates));

        var expectedApportionment = new[]
        {
            new[] { 1, 1, 1, 1, 1, 0, 0, 0, 0 },
            new[] { 2, 3, 1, 1, 2, 1, 0, 2, 0 },
            new[] { 0, 2, 0, 1, 1, 0, 0, 1, 0 },
            new[] { 1, 3, 1, 1, 2, 0, 0, 1, 0 },
            new[] { 1, 1, 2, 1, 1, 0, 0, 0, 0 },
            new[] { 2, 3, 1, 2, 2, 1, 0, 1, 0 },
            new[] { 4, 2, 2, 1, 1, 1, 0, 0, 0 },
            new[] { 1, 1, 1, 1, 1, 0, 1, 0, 0 },
            new[] { 4, 3, 3, 2, 1, 1, 1, 0, 0 },
            new[] { 3, 2, 3, 2, 1, 1, 0, 0, 0 },
            new[] { 3, 1, 2, 1, 1, 1, 1, 0, 1 },
            new[] { 4, 3, 2, 2, 2, 1, 1, 0, 1 },
            new[] { 2, 1, 1, 1, 1, 0, 1, 0, 0 },
            new[] { 2, 3, 2, 2, 2, 0, 1, 1, 0 },
            new[] { 2, 1, 1, 1, 1, 0, 1, 0, 0 },
            new[] { 2, 1, 1, 0, 0, 0, 0, 0, 0 },
            new[] { 6, 3, 3, 2, 1, 1, 1, 0, 1 },
            new[] { 5, 1, 2, 1, 1, 0, 0, 0, 1 },
        };

        var expectedRowDivisors = new[]
        {
            10440.988M, 20269.144M, 8959.863M, 18233.637M,
            13985.150M, 15157.067M, 12025.327M, 12856.3907M,
            25135.371M, 25135.371M, 19834.463M, 25135.371M,
            15524.896M, 24937.287M, 14914.475M, 7262.666M,
            25135.371M, 14398.4875M,
        };

        var expectedColumnDivisors = new[]
        {
            1M,
            0.993M,
            0.927M,
            1.003M,
            1.018M,
            0.983M,
            0.963M,
            0.565M,
            0.678M,
        };

        var expectedResult = new BiproportionalApportionmentExpectedResult(expectedApportionment, expectedRowDivisors, expectedColumnDivisors, 65, 23);
        TestResult(result, expectedResult);
    }

    [Fact]
    public void TestKantonratswahl2015()
    {
        var t = ZhKantonratswahlTestData.Kantonratswahl2015();
        var result = _divisorApportionmentMethod.Calculate(new BiproportionalApportionmentData(t.Weights, t.ElectionNumberOfMandates, t.UnionListNumberOfMandates));

        var expectedApportionment = new[]
        {
            new[] { 1, 1, 1, 0, 1, 0, 0, 0, 0, 0 },
            new[] { 2, 4, 2, 1, 1, 1, 0, 1, 0, 0 },
            new[] { 0, 2, 0, 1, 1, 0, 0, 1, 0, 0 },
            new[] { 2, 3, 1, 1, 1, 0, 0, 1, 0, 0 },
            new[] { 1, 2, 2, 0, 1, 0, 0, 0, 0, 0 },
            new[] { 3, 3, 2, 1, 1, 1, 0, 1, 0, 0 },
            new[] { 4, 2, 3, 1, 0, 1, 0, 0, 0, 0 },
            new[] { 2, 1, 1, 1, 0, 0, 1, 0, 0, 0 },
            new[] { 4, 3, 3, 1, 1, 1, 1, 0, 0, 1 },
            new[] { 4, 2, 4, 1, 1, 1, 0, 0, 0, 0 },
            new[] { 4, 1, 1, 1, 1, 1, 1, 0, 1, 1 },
            new[] { 5, 3, 2, 1, 1, 1, 1, 0, 1, 1 },
            new[] { 3, 1, 1, 0, 0, 0, 1, 0, 1, 0 },
            new[] { 3, 3, 1, 1, 1, 1, 1, 1, 0, 1 },
            new[] { 3, 1, 1, 1, 0, 0, 1, 0, 0, 0 },
            new[] { 2, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 6, 2, 3, 1, 1, 1, 1, 0, 1, 1 },
            new[] { 5, 1, 2, 1, 1, 0, 0, 0, 1, 0 },
        };

        var expectedRowDivisors = new[]
        {
            6246.720M, 16522.738M, 6305.092M, 16021.038M,
            11840.954M, 12434.781M, 11686.046M, 9918.188M,
            24344.051M, 26669.721M, 23060.395M, 24687.896M,
            12280.118M, 26669.721M, 13474.605M, 7271.520M,
            26669.721M, 15125.546M,
        };

        var expectedColumnDivisors = new[] { 1M, 1.028M, 0.976M, 1.047M, 1.022M, 0.961M, 0.949M, 0.782M, 0.674M, 0.627M, };

        var expectedResult = new BiproportionalApportionmentExpectedResult(expectedApportionment, expectedRowDivisors, expectedColumnDivisors, 86, 32);
        TestResult(result, expectedResult);
    }

    [Fact]
    public void TestKantonratswahl2011()
    {
        var t = ZhKantonratswahlTestData.Kantonratswahl2011();
        var result = _divisorApportionmentMethod.Calculate(new BiproportionalApportionmentData(t.Weights, t.ElectionNumberOfMandates, t.UnionListNumberOfMandates));

        var expectedApportionment = new[]
        {
            new[] { 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 },
            new[] { 3, 3, 1, 2, 1, 1, 0, 0, 0, 1 },
            new[] { 0, 2, 0, 1, 1, 0, 0, 0, 0, 1 },
            new[] { 2, 3, 1, 1, 1, 0, 0, 0, 0, 1 },
            new[] { 1, 2, 1, 1, 1, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 1, 1, 1, 1, 1, 0, 0, 0 },
            new[] { 4, 2, 2, 1, 1, 1, 0, 0, 0, 0 },
            new[] { 2, 1, 1, 1, 1, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 2, 1, 2, 1, 1, 1, 0, 0 },
            new[] { 4, 2, 3, 1, 1, 1, 0, 0, 1, 0 },
            new[] { 4, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new[] { 5, 2, 2, 1, 2, 1, 0, 2, 1, 0 },
            new[] { 2, 1, 1, 1, 1, 0, 1, 0, 0, 0 },
            new[] { 3, 3, 1, 2, 1, 1, 1, 1, 0, 0 },
            new[] { 2, 1, 1, 1, 1, 0, 1, 0, 0, 0 },
            new[] { 2, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 6, 3, 2, 1, 1, 1, 1, 1, 1, 0 },
            new[] { 5, 1, 1, 1, 1, 0, 0, 0, 1, 0 },
        };

        var expectedRowDivisors = new[]
        {
            8136.531M, 17107.185M, 7172.762M, 16448.522M,
            10668.135M, 12695.446M, 15790.068M, 14986.901M,
            25519.134M, 28737.905M, 22722.018M, 27654.309M,
            16021.851M, 24951.497M, 15647.942M, 8169.199M,
            26993.054M, 14673.129M,
        };

        var expectedColumnDivisors = new[]
        {
            1M, 1.056M, 1.063M, 1.005M,
            0.977M, 0.950M, 0.769M, 0.633M,
            0.578M, 0.727M,
        };

        var expectedResult = new BiproportionalApportionmentExpectedResult(expectedApportionment, expectedRowDivisors, expectedColumnDivisors, 84, 31);
        TestResult(result, expectedResult);
    }

    [Fact]
    public void TestOneSeatAndTwoListsInLotDecision()
    {
        var voteCountMatrix = new[]
        {
            new[] { 1000, 1000, 1000 },
            new[] { 500, 500, 500 },
        };
        var electionNumberOfMandates = new[] { 8, 4 };
        var unionListNumberOfMandates = new[] { 4, 4, 4 };
        var listDescriptions = new[] { "L1", "L2", "L3" };

        var t = new BiproportionalTestData(voteCountMatrix, electionNumberOfMandates, unionListNumberOfMandates, listDescriptions);
        var result = _divisorApportionmentMethod.Calculate(new BiproportionalApportionmentData(t.Weights, t.ElectionNumberOfMandates, t.UnionListNumberOfMandates));

        // Interpretation of the first column:
        // 1 negative tie => lot decision involves 1 seat. 2 non unique ties => 2 lists are involved in this lot decision
        result.Ties.SelectMany(x => x).SequenceEqual(new[]
        {
            TieState.Positive, TieState.Negative, TieState.Negative,
            TieState.Negative, TieState.Positive, TieState.Positive,
        });
        result.HasTies.Should().BeTrue();
        result.Apportionment.SelectMany(x => x).SequenceEqual(new[]
        {
            2, 3, 3,
            2, 1, 1,
        }).Should().BeTrue();
    }

    [Fact]
    public void TestTwoSeatsAndMultipleListsInLotDecision()
    {
        var voteCountMatrix = new[]
        {
            new[] { 1000, 500 },
            new[] { 1000, 500 },
            new[] { 1000, 500 },
        };
        var electionNumberOfMandates = new[] { 4, 4, 4 };
        var unionListNumberOfMandates = new[] { 8, 4 };
        var listDescriptions = new[] { "L1", "L2", "L3" };

        var t = new BiproportionalTestData(voteCountMatrix, electionNumberOfMandates, unionListNumberOfMandates, listDescriptions);
        var result = _divisorApportionmentMethod.Calculate(new BiproportionalApportionmentData(t.Weights, t.ElectionNumberOfMandates, t.UnionListNumberOfMandates));

        // Tnterpretation of the first column:
        // 2 negative ties => lot decision involves 2 seats. 3 non unique ties => 3 lists from the same list group are involved in this lot decision.
        result.Ties.SelectMany(x => x).SequenceEqual(new[]
        {
            TieState.Negative, TieState.Positive,
            TieState.Negative, TieState.Positive,
            TieState.Positive, TieState.Negative,
        });
        result.HasTies.Should().BeTrue();
        result.Apportionment.SelectMany(x => x).SequenceEqual(new[]
        {
            3, 1,
            3, 1,
            2, 2,
        }).Should().BeTrue();
    }

    private void TestResult(BiproportionalApportionmentResult result, BiproportionalApportionmentExpectedResult expectedResult)
    {
        result.Apportionment.SelectMany(x => x)
            .SequenceEqual(expectedResult.Apportionment.SelectMany(x => x))
            .Should()
            .BeTrue();

        result.RowDivisors.SequenceApproxEqual(expectedResult.RowDivisors, DecimalPrecision)
            .Should()
            .BeTrue();

        result.ColumnDivisors.SequenceApproxEqual(expectedResult.ColumnDivisors, DecimalPrecision)
            .Should()
            .BeTrue();

        result.Ties.SelectMany(x => x).Should().HaveCount(expectedResult.RowDivisors.Length * expectedResult.ColumnDivisors.Length);
        result.Ties.SelectMany(x => x).All(x => x == 0).Should().BeTrue();

        result.NumberOfUpdates.Should().Be(expectedResult.NumberOfUpdates);
        result.NumberOfTransfers.Should().Be(expectedResult.NumberOfTransfers);
    }

    private record BiproportionalApportionmentExpectedResult(int[][] Apportionment, decimal[] RowDivisors, decimal[] ColumnDivisors, int NumberOfUpdates, int NumberOfTransfers);
}
