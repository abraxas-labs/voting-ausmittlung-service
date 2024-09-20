// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional;

public class DoubleProportionalAlgorithmTest
{
    private readonly DoubleProportionalAlgorithm _algorithm = new(new Mock<ILogger<DoubleProportionalAlgorithm>>().Object);

    [Fact]
    public void TestZhKantonratsWahl2023()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateZhKantonratswahl2023();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestZhKantonratsWahl2019()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateZhKantonratswahl2019();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestZhKantonratsWahl2015()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateZhKantonratswahl2015();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestZhGemeinderatsWahl2022()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateZhGemeinderatswahl2022();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestZhGemeinderatsWahl2018()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateZhGemeinderatswahl2018();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestWinterthurStadtParlamentsWahl2022()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateWinterthurStadtparlamentswahl2022();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestWinterthurStadtParlamentsWahl2018()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateWinterthurStadtparlamentswahl2018();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestUster2006()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateUsterStadtparlamentswahl2006();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestWetzikon2022()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateWetzikonStadtparlamentswahl2022();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void SuperApportionmentWithLotDecisionElectionShouldWork()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateSuperApportionmentLotDecisionElectionExample();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
        testOutput.MatchSnapshot();

        var dpResult = electionEndResult.ProportionalElection.DoubleProportionalResult!;

        // The tieState Vector contains:
        // [ - - o o ]
        // 2 minus indicates that 2 values were initially rounded up but were at exactly .5
        dpResult.SuperApportionmentNumberOfMandates.Should().Be(4);
        dpResult.NumberOfMandates.Should().Be(5);
        dpResult.SuperApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.HasOpenLotDecision);
        dpResult.SuperApportionmentNumberOfMandatesForLotDecision.Should().Be(1);

        dpResult.SubApportionmentNumberOfMandates.Should().Be(0);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Unspecified);

        var columns = dpResult.Columns.ToList();
        columns.Should().NotBeEmpty();
        columns.Count(c => c.SuperApportionmentLotDecisionRequired)
            .Should().Be(2);
        columns.Select(c => c.SuperApportionmentNumberOfMandates)
            .SequenceEqual(new[] { 0, 0, 0, 4 })
            .Should().BeTrue();
    }

    [Fact]
    public void SuperApportionmentWithLotDecisionElectionUnionShouldWork()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateSuperApportionmentLotDecisionElectionUnionExample();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();

        var dpResult = unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!;

        dpResult.SuperApportionmentNumberOfMandates.Should().Be(4);
        dpResult.NumberOfMandates.Should().Be(5);
        dpResult.SuperApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.HasOpenLotDecision);
        dpResult.SuperApportionmentNumberOfMandatesForLotDecision.Should().Be(1);

        dpResult.SubApportionmentNumberOfMandates.Should().Be(0);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Initial);

        var columns = dpResult.Columns.ToList();
        columns.Should().NotBeEmpty();
        columns.Count(c => c.SuperApportionmentLotDecisionRequired)
            .Should().Be(2);
        columns.Select(c => c.SuperApportionmentNumberOfMandates)
            .SequenceEqual(new[] { 3, 0, 1 })
            .Should().BeTrue();
    }

    [Fact]
    public void SuperApportionmentWithLotDecisionElectionUnionShouldWorkDetailed()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateSuperApportionmentLotDecisionElectionUnionExample();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!, true);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void SubApportionmentWithLotDecisionElectionUnionShouldWork()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateSubApportionmentLotDecisionElectionUnionExample();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();

        var dpResult = unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!;

        // The tieState Matrix contains:
        // [ o o o ]
        // [ o + - ]
        // [ o - + ]
        // 2 minus indicates that 2 values were initially rounded up but were at exactly .5
        dpResult.SuperApportionmentNumberOfMandates.Should().Be(60);
        dpResult.SubApportionmentNumberOfMandates.Should().Be(58);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.HasOpenLotDecision);

        var columns = dpResult.Columns.ToList();
        columns.Should().NotBeEmpty();
        columns.Any(c => c.SubApportionmentNumberOfMandates != c.SuperApportionmentNumberOfMandates)
            .Should().BeTrue();

        columns.ConvertAll(c => c.SubApportionmentInitialNegativeTies)
            .SequenceEqual(new[] { 0, 1, 1 })
            .Should().BeTrue();

        columns.ConvertAll(c => c.HasSubApportionmentOpenLotDecision)
            .SequenceEqual(new[] { false, true, true })
            .Should().BeTrue();

        var cells = columns.SelectMany(co => co.Cells).WhereNotNull().ToList();
        cells.Should().NotBeEmpty();
        cells.ConvertAll(c => c.SubApportionmentLotDecisionRequired)
            .SequenceEqual(new[] { false, false, false, false, true, true, false, true, true })
            .Should().BeTrue();

        foreach (var cell in cells)
        {
            cell.ListId = Guid.Empty;
        }
    }

    [Fact]
    public void SuperAndSubApportionmentLotDecisionsShouldWork()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateSuperAndSubApportionmentLotDecisions();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = DoubleProportionalAlgorithmTestUtils.BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!, true);
        testOutput.MatchSnapshot();
    }
}
