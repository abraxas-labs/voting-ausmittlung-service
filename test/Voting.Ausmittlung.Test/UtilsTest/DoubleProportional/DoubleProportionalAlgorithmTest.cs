// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.UtilsTest.DoubleProportional.Models;
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
        var testOutput = BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestZhKantonratsWahl2019()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateZhKantonratswahl2019();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestZhGemeinderatsWahl2022()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateZhGemeinderatswahl2022();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestZhGemeinderatsWahl2018()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateZhGemeinderatswahl2018();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestWinterthurStadtParlamentsWahl2022()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateWinterthurStadtparlamentswahl2022();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestWinterthurStadtParlamentsWahl2018()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateWinterthurStadtparlamentswahl2018();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void TestUster2006()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateUsterStadtparlamentswahl2006();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void SuperApportionmentWithLotDecisionElectionShouldWork()
    {
        var electionEndResult = DoubleProportionalAlgorithmTestData.GenerateSuperApportionmentLotDecisionElectionExample();
        _algorithm.BuildResultForElection(electionEndResult.ProportionalElection);
        var testOutput = BuildTestOutput(electionEndResult.ProportionalElection.DoubleProportionalResult!);
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
        var testOutput = BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
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
        var testOutput = BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!, true);
        testOutput.MatchSnapshot();
    }

    [Fact]
    public void SubApportionmentWithLotDecisionElectionUnionShouldWork()
    {
        var unionEndResult = DoubleProportionalAlgorithmTestData.GenerateSubApportionmentLotDecisionElectionUnionExample();
        _algorithm.BuildResultForUnion(unionEndResult.ProportionalElectionUnion);
        var testOutput = BuildTestOutput(unionEndResult.ProportionalElectionUnion.DoubleProportionalResult!);
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

    private DoubleProportionalAlgorithmTestOutput BuildTestOutput(DoubleProportionalResult dpResult, bool includeSubApportionmentPermutations = false)
    {
        // Is required to simulate lot decisions.
        SetMockIds(dpResult);

        var result = new DoubleProportionalAlgorithmTestOutput();
        var columnsWithAnyRequiredQuorumReached = dpResult.Columns.Where(c => c.AnyRequiredQuorumReached).ToList();

        result.CantonalQuorum = dpResult.CantonalQuorum;
        result.VoteCount = dpResult.VoteCount;
        result.NumberOfMandates = dpResult.NumberOfMandates;

        result.ColumnVoteCounts = dpResult.Columns.Select(x => x.VoteCount).ToArray();
        result.RowVoteCounts = dpResult.Rows.Select(x => x.VoteCount).ToArray();
        result.UnionListAnyQuorumReachedList = dpResult.Columns.Select(x => x.AnyRequiredQuorumReached).ToArray();

        result.ElectionKey = dpResult.ElectionKey;
        result.TotalVoterNumber = dpResult.VoterNumber;
        result.ColumnVoterNumbers = dpResult.Columns.Select(x => x.VoterNumber).ToArray();

        result.SuperApportionment = new()
        {
            State = dpResult.SuperApportionmentState,
            NumberOfMandates = dpResult.SuperApportionmentNumberOfMandates,
            NumberOfMandatesForLotDecision = dpResult.SuperApportionmentNumberOfMandatesForLotDecision,
            NumberOfMandatesExclLotDecisionVector = dpResult.Columns.Select(x => x.SuperApportionmentNumberOfMandates).ToArray(),
            SubApportionments = new(),
        };

        result.SuperApportionment.NumberOfMandatesVectors = new[] { result.SuperApportionment.NumberOfMandatesExclLotDecisionVector };
        var superApportionmentLotDecisions = DoubleProportionalAlgorithmLotDecisionsBuilder.BuildSuperApportionmentLotDecisions(dpResult);

        if (dpResult.SuperApportionmentState == DoubleProportionalResultApportionmentState.HasOpenLotDecision)
        {
            result.SuperApportionment.LotDecisionRequiredVector = dpResult.Columns.Select(x => x.SuperApportionmentLotDecisionRequired).ToArray();
            result.SuperApportionment.NumberOfMandatesVectors = new int[superApportionmentLotDecisions.Count][];

            for (var lotDecisionIndex = 0; lotDecisionIndex < superApportionmentLotDecisions.Count; lotDecisionIndex++)
            {
                var lotDecisionColumns = superApportionmentLotDecisions[lotDecisionIndex].Columns.ToList();
                var numberOfMandatesInclLotDecisionVector = result.SuperApportionment.NumberOfMandatesExclLotDecisionVector.ToArray();

                for (var i = 0; i < numberOfMandatesInclLotDecisionVector.Length; i++)
                {
                    var column = columnsWithAnyRequiredQuorumReached[i];
                    var lotDecisionColumn = lotDecisionColumns.FirstOrDefault(c => (c.UnionList?.Id ?? c.List!.Id) == (column.UnionListId ?? column.ListId));
                    numberOfMandatesInclLotDecisionVector[i] = lotDecisionColumn?.NumberOfMandates ?? numberOfMandatesInclLotDecisionVector[i];
                }

                result.SuperApportionment.NumberOfMandatesVectors[lotDecisionIndex] = numberOfMandatesInclLotDecisionVector;
            }
        }

        if (dpResult.ProportionalElectionId != null)
        {
            return result;
        }

        if (dpResult.SuperApportionmentState != DoubleProportionalResultApportionmentState.HasOpenLotDecision || !includeSubApportionmentPermutations)
        {
            result.SuperApportionment.SubApportionments.Add(BuildSubApportionmentTestOutput(dpResult));
            return result;
        }

        // Each super apportionment lot decision will lead to a different sub apportionment.
        // Note: VOTING does not directly support this feature, but we generate it here for testing purposes.
        // In VOTING it is always step per step: Trigger Double Proportional Calculation -> Set SuperApportionment Lot Decision -> Calculate SubApportionment -> Set SubApportionment Lot Decision.
        for (var superApportionmentVariant = 0; superApportionmentVariant < result.SuperApportionment.NumberOfMandatesVectors.Length; superApportionmentVariant++)
        {
            _algorithm.SetSuperApportionmentLotDecision(dpResult, new()
            {
                Columns = superApportionmentLotDecisions[superApportionmentVariant]
                    .Columns
                    .Select(co => new Core.Domain.DoubleProportionalResultSuperApportionmentLotDecisionColumn
                    {
                        ListId = co.List?.Id,
                        UnionListId = co.UnionList?.Id,
                        NumberOfMandates = co.NumberOfMandates,
                    })
                    .ToList(),
            });

            result.SuperApportionment.SubApportionments.Add(BuildSubApportionmentTestOutput(dpResult));
        }

        return result;
    }

    private DoubleProportionalAlgorithmTestOutputSubApportionment BuildSubApportionmentTestOutput(DoubleProportionalResult dpResult)
    {
        var subApportionment = new DoubleProportionalAlgorithmTestOutputSubApportionment
        {
            State = dpResult.SubApportionmentState,
            NumberOfMandates = dpResult.SubApportionmentNumberOfMandates,
            RowDivisors = dpResult.Rows.Select(r => r.Divisor).ToArray(),
            ColumnDivisors = dpResult.Columns.Select(c => c.Divisor).ToArray(),
        };

        var columnsWithSeats = dpResult.Columns.Where(c => c.SuperApportionmentNumberOfMandates > 0).ToList();
        var rows = dpResult.Rows.ToList();

        subApportionment.NumberOfMandatesExclLotDecisionMatrix = BuildMatrix(
            rows,
            columnsWithSeats,
            x => x.SubApportionmentNumberOfMandatesExclLotDecision);

        subApportionment.NumberOfMandatesMatrices = new[]
        {
            subApportionment.NumberOfMandatesExclLotDecisionMatrix,
        };

        if (dpResult.SubApportionmentState != DoubleProportionalResultApportionmentState.HasOpenLotDecision)
        {
            return subApportionment;
        }

        subApportionment.LotDecisionRequiredMatrix = BuildMatrix(
            rows,
            columnsWithSeats,
            x => x.SubApportionmentLotDecisionRequired);

        var subApportionmentLotDecisions = DoubleProportionalAlgorithmLotDecisionsBuilder.BuildSubApportionmentLotDecisions(dpResult);
        subApportionment.NumberOfMandatesMatrices = new int[subApportionmentLotDecisions.Count][][];

        for (var lotDecisionIndex = 0; lotDecisionIndex < subApportionmentLotDecisions.Count; lotDecisionIndex++)
        {
            var lotDecisionCells = subApportionmentLotDecisions[lotDecisionIndex].Columns.SelectMany(co => co.Cells).ToList();

            var numberOfMandatesInclLotDecisionMatrix = subApportionment.NumberOfMandatesExclLotDecisionMatrix.ToArray();
            for (var y = 0; y < numberOfMandatesInclLotDecisionMatrix.Length; y++)
            {
                numberOfMandatesInclLotDecisionMatrix[y] = numberOfMandatesInclLotDecisionMatrix[y].ToArray();
                for (var x = 0; x < numberOfMandatesInclLotDecisionMatrix[0].Length; x++)
                {
                    var cell = columnsWithSeats[x].Cells.ElementAtOrDefault(y);
                    if (cell == null)
                    {
                        continue;
                    }

                    var lotDecisionCell = lotDecisionCells.Find(c => c.List.Id == cell.ListId);
                    numberOfMandatesInclLotDecisionMatrix[y][x] = lotDecisionCell?.NumberOfMandates ?? numberOfMandatesInclLotDecisionMatrix[y][x];
                }
            }

            subApportionment.NumberOfMandatesMatrices[lotDecisionIndex] = numberOfMandatesInclLotDecisionMatrix.ToArray();
        }

        return subApportionment;
    }

    private TOut?[][] BuildMatrix<TOut>(
        List<DoubleProportionalResultRow> rows,
        List<DoubleProportionalResultColumn> columns,
        Func<DoubleProportionalResultCell, TOut> mapperFn)
    {
        var matrix = new TOut?[rows.Count][];

        for (var y = 0; y < rows.Count; y++)
        {
            var rowCells = rows[y].Cells.ToList();
            matrix[y] = new TOut[columns.Count];

            for (var x = 0; x < columns.Count; x++)
            {
                var column = columns[x];
                var cell = column.Cells.FirstOrDefault(rowCells.Contains);

                matrix[y][x] = cell != null ? mapperFn(cell) : default;
            }
        }

        return matrix;
    }

    private void SetMockIds(DoubleProportionalResult dpResult)
    {
        foreach (var column in dpResult.Columns)
        {
            column.Id = Guid.NewGuid();

            foreach (var cell in column.Cells)
            {
                cell.Id = Guid.NewGuid();
                cell.ColumnId = column.Id;
                cell.List = new() { Id = cell.ListId };
            }
        }

        foreach (var row in dpResult.Rows)
        {
            row.Id = Guid.NewGuid();

            foreach (var cell in row.Cells)
            {
                cell.RowId = row.Id;
            }
        }
    }
}
