// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.Utils;
using Voting.Ausmittlung.Test.UtilsTest.DoubleProportional.Models;

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional;

public static class DoubleProportionalAlgorithmTestData
{
    private static readonly string SnapshotFolderPath = Path.Combine(TestSourcePaths.TestProjectSourceDirectory, "UtilsTest", "DoubleProportional", "TestFiles");

    public static ProportionalElectionUnionEndResult GenerateZhKantonratswahl2023()
    {
        return GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-kantonratswahl-2023.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateZhKantonratswahl2019()
    {
        return GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-kantonratswahl-2019.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateZhGemeinderatswahl2022()
    {
        return GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-gemeinderatswahl-2022.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateZhGemeinderatswahl2018()
    {
        return GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-gemeinderatswahl-2018.json"));
    }

    public static ProportionalElectionEndResult GenerateWinterthurStadtparlamentswahl2018()
    {
        return GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("winterthur-stadtparlamentswahl-2018.json"));
    }

    public static ProportionalElectionEndResult GenerateWinterthurStadtparlamentswahl2022()
    {
        return GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("winterthur-stadtparlamentswahl-2022.json"));
    }

    public static ProportionalElectionEndResult GenerateSuperApportionmentLotDecisionElectionExample()
    {
        return GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("super-apportionment-lot-decision-election.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateSuperApportionmentLotDecisionElectionUnionExample()
    {
        return GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("super-apportionment-lot-decision-election-union.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateSubApportionmentLotDecisionElectionUnionExample()
    {
        return GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("sub-apportionment-lot-decision-election-union.json"));
    }

    public static ProportionalElectionEndResult GenerateUsterStadtparlamentswahl2006()
    {
        return GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("uster-2006.json"));
    }

    private static DoubleProportionalAlgorithmTestInput GetDoubleProportionalAlgorithmInput(string fileName)
    {
        var json = File.ReadAllText(Path.Combine(SnapshotFolderPath, fileName));
        return JsonSerializer.Deserialize<DoubleProportionalAlgorithmTestInput>(json)!;
    }

    private static ProportionalElectionUnionEndResult GenerateUnionEndResult(DoubleProportionalAlgorithmTestInput input)
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

    private static ProportionalElectionEndResult GenerateElectionEndResult(DoubleProportionalAlgorithmTestInput input)
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

    private record MatrixRow(string PoliticalBusinessNumber, string Name, int NumberOfMandates);

    private record MatrixCol(string OrderNumber, string Name);
}
