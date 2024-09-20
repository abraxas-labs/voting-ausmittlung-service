// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.Utils;
using Voting.Ausmittlung.Test.UtilsTest.DoubleProportional.Models;

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional;

public static class DoubleProportionalAlgorithmTestUtils
{
    private static readonly DoubleProportionalAlgorithm _algorithm = new(new Mock<ILogger<DoubleProportionalAlgorithm>>().Object);

    public static DoubleProportionalAlgorithmTestInput GetDoubleProportionalAlgorithmInput(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<DoubleProportionalAlgorithmTestInput>(json)!;
    }

    public static ProportionalElectionUnionEndResult GenerateUnionEndResult(DoubleProportionalAlgorithmTestInput input)
    {
        var union = new ProportionalElectionUnion
        {
            EndResult = new(),
            Contest = new(),
        };

        var cols = input.Columns;
        var rows = input.Rows;

        var elections = rows.Select((y, i) => new ProportionalElection
        {
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                y.Label,
                (t, s) => t.ShortDescription = s,
                y.Label),
            NumberOfMandates = y.NumberOfMandates,
            PoliticalBusinessNumber = (i + 1).ToString("D2"),
            EndResult = new(),
            MandateAlgorithm = input.MandateAlgorithm,
        }).ToList();

        var unionLists = cols.Select((x, i) => new ProportionalElectionUnionList
        {
            Id = Guid.NewGuid(),
            OrderNumber = (i + 1).ToString("D2"),
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionUnionListTranslation>(
                (t, s) => t.ShortDescription = s,
                x.Label),
        }).ToList();

        for (var y = 0; y < rows.Count; y++)
        {
            var rowVoteCounts = input.VoteCounts[y];

            for (var x = 0; x < cols.Count; x++)
            {
                var election = elections[y];
                var listVoteCount = rowVoteCounts[x];

                if (listVoteCount == null)
                {
                    continue;
                }

                var list = new ProportionalElectionList
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = (x + 1).ToString("D2"),
                    Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                        (t, s) => t.ShortDescription = s,
                        cols[x].Label,
                        (t, s) => t.Description = s,
                        cols[x].Label),
                    ProportionalElection = election,
                    EndResult = new()
                    {
                        Id = Guid.NewGuid(),
                        ConventionalSubTotal = new()
                        {
                            UnmodifiedListVotesCount = listVoteCount!.Value,
                        },
                    },
                };

                list.EndResult.List = list;
                list.EndResult.ListId = list.Id;
                election.ProportionalElectionLists.Add(list);
            }
        }

        foreach (var election in elections)
        {
            election.EndResult!.ProportionalElection = election;
            election.EndResult!.ListEndResults = election.ProportionalElectionLists!.Select(x => x.EndResult!).ToList();
        }

        foreach (var unionList in unionLists)
        {
            var lists = elections.SelectMany(e => e.ProportionalElectionLists)
                .Where(l => l.OrderNumber == unionList.OrderNumber)
                .ToList();
            unionList.ProportionalElectionUnionListEntries = lists
                .ConvertAll(l => new ProportionalElectionUnionListEntry { ProportionalElectionList = l, ProportionalElectionListId = l.Id });
        }

        union.ProportionalElectionUnionLists = unionLists;
        union.EndResult.ProportionalElectionUnion = union;
        union.ProportionalElectionUnionEntries = elections
            .ConvertAll(e => new ProportionalElectionUnionEntry { ProportionalElection = e, });

        union.EndResult.TotalCountOfElections = elections.Count;
        union.EndResult.CountOfDoneElections = elections.Count;
        return union.EndResult;
    }

    public static ProportionalElectionEndResult GenerateElectionEndResult(DoubleProportionalAlgorithmTestInput input)
    {
        var row = input.Rows[0];
        var cols = input.Columns.ToArray();

        var election = new ProportionalElection
        {
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                row.Label,
                (t, s) => t.ShortDescription = s,
                row.Label),
            NumberOfMandates = row.NumberOfMandates,
            PoliticalBusinessNumber = row.Label,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum,
            Contest = new(),
        };

        var voteCounts = input.VoteCounts.ToArray()[0];
        for (var x = 0; x < cols.Length; x++)
        {
            var listVoteCount = voteCounts[x];

            if (listVoteCount == null)
            {
                continue;
            }

            var list = new ProportionalElectionList
            {
                Id = Guid.NewGuid(),
                OrderNumber = (x + 1).ToString("D2"),
                Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                    (t, s) => t.ShortDescription = s,
                    cols[x].Label,
                    (t, s) => t.Description = s,
                    cols[x].Label),
                ProportionalElection = election,
                EndResult = new()
                {
                    Id = Guid.NewGuid(),
                    ConventionalSubTotal = new()
                    {
                        UnmodifiedListVotesCount = listVoteCount.Value,
                    },
                },
            };

            list.EndResult.List = list;
            list.EndResult.ListId = list.Id;
            election.ProportionalElectionLists.Add(list);
        }

        var endResult = new ProportionalElectionEndResult
        {
            ProportionalElection = election,
            ListEndResults = election.ProportionalElectionLists.Select(l => l.EndResult!).ToList(),
        };

        endResult.ProportionalElection.EndResult = endResult;
        return endResult;
    }

    public static DoubleProportionalAlgorithmTestOutput BuildTestOutput(DoubleProportionalResult dpResult, bool includeSubApportionmentPermutations = false)
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

            foreach (var column in dpResult.Columns)
            {
                column.SubApportionmentInitialNegativeTies = 0;
                column.SubApportionmentNumberOfMandates = 0;

                foreach (var cell in column.Cells)
                {
                    cell.SubApportionmentLotDecisionRequired = false;
                    cell.SubApportionmentNumberOfMandatesExclLotDecision = 0;
                    cell.SubApportionmentNumberOfMandatesFromLotDecision = 0;
                }
            }
        }

        return result;
    }

    public static DoubleProportionalAlgorithmTestOutputSubApportionment BuildSubApportionmentTestOutput(DoubleProportionalResult dpResult)
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

    public static TOut?[][] BuildMatrix<TOut>(
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

    public static void SetMockIds(DoubleProportionalResult dpResult)
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
