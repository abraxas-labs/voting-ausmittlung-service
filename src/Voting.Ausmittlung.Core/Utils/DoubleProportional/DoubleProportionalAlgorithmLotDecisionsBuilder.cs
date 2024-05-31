// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.DoubleProportional;

public static class DoubleProportionalAlgorithmLotDecisionsBuilder
{
    public static List<DoubleProportionalResultSuperApportionmentLotDecision> BuildSuperApportionmentLotDecisions(DoubleProportionalResult dpResult)
    {
        if (!dpResult.HasSuperApportionmentRequiredLotDecision)
        {
            return new();
        }

        var columnsWithRequiredLotDecision = dpResult.Columns
            .Where(co => co.SuperApportionmentLotDecisionRequired)
            .ToList();

        if (columnsWithRequiredLotDecision.Count < dpResult.SuperApportionmentNumberOfMandatesForLotDecision)
        {
            throw new ValidationException("Cannot build lot decisions when there are more number of mandates to distribute per lot decision than having columns");
        }

        // A lot number of mandates vector here is an array filled with 0 or 1's. A 1 means that the column receives 1 seat.
        // Ex: [1, 1, 0, 0] means that totally 2 seats have to be distributed per lot decision and the first 2 columns receive a seat.
        var initialLotNumberOfMandatesVector = Enumerable.Range(0, columnsWithRequiredLotDecision.Count)
            .Select(i => i < dpResult.SuperApportionmentNumberOfMandatesForLotDecision ? 1 : 0)
            .ToList();

        var lotNumberOfMandatesVectors = PermutationUtil.GenerateUniquePermutations(initialLotNumberOfMandatesVector);
        var lotDecisions = new List<DoubleProportionalResultSuperApportionmentLotDecision>();

        for (var lotDecisionNumber = 1; lotDecisionNumber <= lotNumberOfMandatesVectors.Count; lotDecisionNumber++)
        {
            var lotNumberOfMandatesVector = lotNumberOfMandatesVectors.ElementAt(lotDecisionNumber - 1);
            var lotDecisionColumns = lotNumberOfMandatesVector
                .Select((_, i) => columnsWithRequiredLotDecision[i])
                .Select((column, i) => new DoubleProportionalResultSuperApportionmentLotDecisionColumn(
                    column.UnionListId != null ? column.UnionList ?? new() { Id = column.UnionListId.Value } : null,
                    column.ListId != null ? column.List ?? new() { Id = column.ListId.Value } : null,
                    column.SuperApportionmentNumberOfMandatesExclLotDecision + lotNumberOfMandatesVector.ElementAt(i)))
                .ToList();

            lotDecisions.Add(new DoubleProportionalResultSuperApportionmentLotDecision(lotDecisionNumber, lotDecisionColumns));
        }

        return lotDecisions;
    }

    public static List<DoubleProportionalResultSubApportionmentLotDecision> BuildSubApportionmentLotDecisions(DoubleProportionalResult dpResult)
    {
        if (!dpResult.HasSubApportionmentRequiredLotDecision)
        {
            return new();
        }

        var electionByCellId = new Dictionary<Guid, ProportionalElection>();

        foreach (var row in dpResult.Rows)
        {
            foreach (var cell in row.Cells)
            {
                electionByCellId.Add(cell.Id, row.ProportionalElection ?? new() { Id = row.ProportionalElectionId });
            }
        }

        var validLotNumberOfMandatesVectorsList = BuildValidSubApportionmentLotNumberOfMandatesVectorsList(dpResult);

        var columnsWithRequiredLotDecision = dpResult.Columns
            .Where(co => co.Cells.Any(ce => ce.SubApportionmentLotDecisionRequired))
            .ToList();

        // Builds lot decisions.
        var lotDecisions = new List<DoubleProportionalResultSubApportionmentLotDecision>();
        for (var lotDecisionNumber = 1; lotDecisionNumber <= validLotNumberOfMandatesVectorsList.Count; lotDecisionNumber++)
        {
            var lotNumberOfMandatesVectors = validLotNumberOfMandatesVectorsList.ElementAt(lotDecisionNumber - 1);
            var lotDecisionColumns = new List<DoubleProportionalResultSubApportionmentLotDecisionColumn>();

            for (var colIndex = 0; colIndex < columnsWithRequiredLotDecision.Count; colIndex++)
            {
                var column = columnsWithRequiredLotDecision[colIndex];
                var lotDecisionColumnCells = column.Cells
                    .Where(ce => ce.SubApportionmentLotDecisionRequired)
                    .Select((ce, ceIndex) => new DoubleProportionalResultSubApportionmentLotDecisionCell(
                        electionByCellId[ce.Id],
                        ce.List,
                        ce.SubApportionmentNumberOfMandatesExclLotDecision + lotNumberOfMandatesVectors.ElementAt(colIndex).ElementAt(ceIndex)))
                    .ToList();

                lotDecisionColumns.Add(new DoubleProportionalResultSubApportionmentLotDecisionColumn(
                    column.UnionList,
                    column.List,
                    lotDecisionColumnCells));
            }

            lotDecisions.Add(new DoubleProportionalResultSubApportionmentLotDecision(lotDecisionNumber, lotDecisionColumns));
        }

        return lotDecisions;
    }

    private static IReadOnlyCollection<IReadOnlyCollection<IReadOnlyCollection<int>>> BuildValidSubApportionmentLotNumberOfMandatesVectorsList(DoubleProportionalResult dpResult)
    {
        var expectedNumberOfMandatesPerLotDecisionByRowId = new Dictionary<Guid, int>();

        var columnsWithRequiredLotDecision = dpResult.Columns
            .Where(co => co.Cells.Any(ce => ce.SubApportionmentLotDecisionRequired))
            .ToList();

        // Calculate the expected number of mandates per row. A lot is only valid if it matches the row criteria.
        // The column criteria is always implicitly guaranteed.
        for (var x = 0; x < dpResult.Rows.Count; x++)
        {
            var row = dpResult.Rows.ElementAt(x);
            if (!row.Cells.Any(ce => ce.SubApportionmentLotDecisionRequired))
            {
                continue;
            }

            var rowSubApportionmentNumberOfMandatesExclLotDecision = row.Cells
                .Sum(ce => ce.SubApportionmentNumberOfMandatesExclLotDecision);

            expectedNumberOfMandatesPerLotDecisionByRowId[row.Id] = row.NumberOfMandates - rowSubApportionmentNumberOfMandatesExclLotDecision;
        }

        // Calculates all possible lots per column.
        // Ex: Column C1 with [1,0] and Column C2 with [1,0] =
        // C1 => [[1,0],[0,1]], C2 => [[1,0],[0,1]]
        var lotNumberOfMandatesVectorsByColumnId = new Dictionary<Guid, IReadOnlyCollection<IReadOnlyCollection<int>>>();
        foreach (var column in dpResult.Columns)
        {
            if (column.SubApportionmentInitialNegativeTies == 0)
            {
                continue;
            }

            var columnCellsWithRequiredLotDecision = column.Cells
                .Where(ce => ce.SubApportionmentLotDecisionRequired)
                .ToList();

            var initialLotNumberOfMandatesVector = Enumerable.Range(0, columnCellsWithRequiredLotDecision.Count)
                .Select(i => i < column.SubApportionmentInitialNegativeTies ? 1 : 0)
                .ToList();

            lotNumberOfMandatesVectorsByColumnId[column.Id] = PermutationUtil.GenerateUniquePermutations(initialLotNumberOfMandatesVector);
        }

        var cellsWithRequiredLotDecision = columnsWithRequiredLotDecision
            .SelectMany(co => co.Cells)
            .Where(ce => ce.SubApportionmentLotDecisionRequired)
            .ToList();

        // Generates all valid lots (which match the row criteria).
        // If we have the column lots C1 => [[1,0],[0,1]], C2 => [[1,0],[0,1]] and the row criteria is 1 for both rows =
        // 1. valid variant is [1,0] from C1 and [0,1] from C2
        // 2. valid variant is [0,1] from C1 and [1,0] from C2
        return CombinationsUtil.GenerateCombinations(
            lotNumberOfMandatesVectorsByColumnId.Values.ToList(),
            combinations => FilterSubApportionmentColumnLotCombinations(combinations, cellsWithRequiredLotDecision, expectedNumberOfMandatesPerLotDecisionByRowId));
    }

    private static bool FilterSubApportionmentColumnLotCombinations(
        IReadOnlyCollection<int>[] combinations,
        List<DoubleProportionalResultCell> cellsWithRequiredLotDecision,
        Dictionary<Guid, int> expectedNumberOfMandatesPerLotDecisionByRowId)
    {
        var flattenedCombinations = combinations.SelectMany(c => c).ToList();
        var numberOfMandatesPerLotDecisionByRowId = new Dictionary<Guid, int>();

        for (var i = 0; i < cellsWithRequiredLotDecision.Count; i++)
        {
            var cell = cellsWithRequiredLotDecision[i];
            var numberOfMandatesPerLotDecision = flattenedCombinations[i];

            if (!numberOfMandatesPerLotDecisionByRowId.TryAdd(cell.RowId, numberOfMandatesPerLotDecision))
            {
                numberOfMandatesPerLotDecisionByRowId[cell.RowId] += numberOfMandatesPerLotDecision;
            }
        }

        return numberOfMandatesPerLotDecisionByRowId.SequenceEqual(expectedNumberOfMandatesPerLotDecisionByRowId);
    }
}
