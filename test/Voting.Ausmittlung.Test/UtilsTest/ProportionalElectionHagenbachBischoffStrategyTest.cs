// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Core.Utils.ProportionalElectionStrategy;
using Voting.Ausmittlung.Data.Models;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class ProportionalElectionHagenbachBischoffStrategyTest
{
    private const string ListId1 = "23f60b53-a3aa-414a-82b0-b020e7406c9d";
    private const string ListId2 = "38e98e08-3262-465f-a29f-c9d106403e4e";
    private const string ListId3 = "0f309326-e787-4c8c-b631-309dff5ca69e";
    private const string ListId4 = "35eaea5f-93c3-443b-a84b-5241c74e0ee0";
    private const string ListId5 = "c0a73fe9-6e84-475c-82e2-4f3aa7570fb8";
    private const string ListId6 = "69d8d415-04f9-4d26-8585-013b364a6b29";
    private const string ListId7 = "09c7ddcb-8876-442d-b922-427f67a11479";
    private const string ListId8 = "96b3f7d9-dcbb-496c-b9ef-e5739a1a67a8";
    private const string ListId9 = "69c9431a-7139-47d2-b5d6-e34141cef2dd";

    private const string ListUnionId1 = "a52ad8d2-a9a9-4e7b-ba2a-fbba116251d2";
    private const string ListUnionId2 = "cb68087c-fcd7-40b2-96c3-216d907f75fd";
    private const string ListUnionId3 = "7d016ce3-92fb-450a-bcb2-9bbca9e7fb7b";
    private const string SubListUnionId1 = "8af867a9-b447-4a7f-99e8-4bf28341ca0e";
    private const string SubListUnionId2 = "cb042882-81d9-42b9-a135-2722fefb3e67";

    /// <summary>
    /// Test with the sample datas according the attachment Slides_Mediengespraech_NR_SR_2019.pptx in jira ticket 201.
    /// </summary>
    [Fact]
    public void TestSlidesMediengespraechSg2019()
    {
        var endResult = GetBasicEndResult(
            5,
            new SimplifiedList(ListId1, 13000, ListUnionId1, SubListUnionId1),
            new SimplifiedList(ListId2, 7000, ListUnionId1, SubListUnionId1),
            new SimplifiedList(ListId3, 5000, ListUnionId1, SubListUnionId2),
            new SimplifiedList(ListId4, 3000, ListUnionId1, SubListUnionId2),
            new SimplifiedList(ListId5, 12000, ListUnionId2),
            new SimplifiedList(ListId6, 5000, ListUnionId2),
            new SimplifiedList(ListId7, 10000, ListUnionId3),
            new SimplifiedList(ListId8, 3000, ListUnionId3),
            new SimplifiedList(ListId9, 2000, ListUnionId3));

        ProportionalElectionHagenbachBischoffStrategy.RecalculateNumberOfMandatesForLists(endResult);

        // validate nr of mandates (end result)
        endResult.ListEndResults
            .Select(x => x.NumberOfMandates)
            .Should()
            .Equal(1, 1, 1, 0, 1, 0, 1, 0, 0);

        // validate group calculations / calculation rounds
        var rootGroup = endResult.HagenbachBischoffRootGroup!;
        rootGroup.SortCalculationRounds();
        rootGroup.Quotient.Should().Be(10_000);
        rootGroup.DistributionNumber.Should().Be(10_001);

        var groups = rootGroup.Children.ToList();

        groups.Select(x => x.NumberOfMandates)
            .Should()
            .Equal(3, 1, 1);

        groups.Select(x => x.Quotient)
            .Should()
            .Equal(7000, 8500, 7500);

        groups.Select(x => x.VoteCount)
            .Should()
            .Equal(28_000, 17_000, 15_000);

        groups.Select(x => x.InitialNumberOfMandates)
            .Should()
            .Equal(2, 1, 1);

        groups.Select(x => x.NumberOfMandates)
            .Should()
            .Equal(3, 1, 1);

        var rootCalculationRounds = rootGroup.CalculationRounds.ToList();
        var groupValues = rootCalculationRounds.SelectMany(x => x.GroupValues).ToList();
        groupValues.Select(x => x.PreviousQuotient)
            .Should()
            .Equal(28_000 / 3M, 8500M, 7500M);
        groupValues.Select(x => x.NextQuotient)
            .Should()
            .Equal(7000M, 8500M, 7500M);
        groupValues.Select(x => x.NumberOfMandates)
            .Should()
            .Equal(3, 1, 1);
        groupValues.Select(x => x.PreviousNumberOfMandates)
            .Should()
            .Equal(2, 1, 1);
        rootCalculationRounds.Select(x => x.WinnerReason)
            .Should()
            .Equal(HagenbachBischoffCalculationRoundWinnerReason.Quotient);
        rootCalculationRounds.Select(x => x.Winner.ListUnionId.ToString())
            .Should()
            .Equal(ListUnionId1);

        // validate calculations inside list union 1 ABCD
        var listUnion1Group = groups[0];
        listUnion1Group.Children.Select(x => x.VoteCount)
            .Should()
            .Equal(20_000, 8_000);
        listUnion1Group.Children.Select(x => x.Quotient)
            .Should()
            .Equal(20_000 / 3M, 4000M);
        listUnion1Group.Children.Select(x => x.InitialNumberOfMandates)
            .Should()
            .Equal(2, 1);
        listUnion1Group.Children.Select(x => x.NumberOfMandates)
            .Should()
            .Equal(2, 1);

        // validate calculations inside list subunion 1
        var listSubUnion1Group = listUnion1Group.Children.First();
        listSubUnion1Group.Children.Select(x => x.VoteCount)
            .Should()
            .Equal(13_000, 7_000);
        listSubUnion1Group.Children.Select(x => x.Quotient)
            .Should()
            .Equal(6500M, 3500M);
        listSubUnion1Group.Children.Select(x => x.InitialNumberOfMandates)
            .Should()
            .Equal(1, 1);
        listSubUnion1Group.Children.Select(x => x.NumberOfMandates)
            .Should()
            .Equal(1, 1);
    }

    /// <summary>
    /// test according the attachment "Proporzwahl - Ebene Bund - 5a_Verteilung der Sitze.pdf" in jira ticket VOTING-302.
    /// </summary>
    [Fact]
    public void TestVoting302ReportExampleData()
    {
        var listUnionId1 = "fc6d7852-d198-47ba-b340-b3f4d51e5fc4";
        var listUnionId2 = "6cbf01b0-029f-4c60-bf74-0bc45682d1a5";
        var listUnionId3 = "a99cc832-a680-498a-a8f9-e0136cc19240";
        var listUnionId4 = "746ffba9-f87a-40a3-a8dc-f49da6c88899";
        var listUnionId5 = "2efa7189-461a-4123-82ba-0fc89ccb0b35";
        var listSubUnionId1 = "7e76c59f-0c5b-4da3-b5c2-9f8489f18938";
        var listSubUnionId2 = "02d3702d-2fd8-4da2-9ba7-dc30faf5a94a";
        var listSubUnionId3 = "854bbaf0-d623-456b-9a05-314f5b71ec86";
        var listSubUnionId4 = "927db38e-9efe-422e-b7e0-0e01d02d56a5";
        var listSubUnionId5 = "ba796b52-eeec-42c0-8af6-f36ce8d9c3f0";
        var endResult = GetBasicEndResult(
            12,
            /* 01 */ new SimplifiedList(26_843, listUnionId1, listSubUnionId1),
            /* 02 */ new SimplifiedList(18_024, listUnionId2),
            /* 03 */ new SimplifiedList(259_922, listUnionId1, listSubUnionId1),
            /* 04 */ new SimplifiedList(30_333, listUnionId1),
            /* 05 */ new SimplifiedList(8_519),
            /* 06 */ new SimplifiedList(57_133, listUnionId1, listSubUnionId2),
            /* 07 */ new SimplifiedList(214_205, listUnionId2),
            /* 08 */ new SimplifiedList(620_183, listUnionId3),
            /* 09 */ new SimplifiedList(231_302, listUnionId4, listSubUnionId3),
            /* 10 */ new SimplifiedList(14_356, listUnionId4, listSubUnionId3),
            /* 11 */ new SimplifiedList(76_606, listUnionId4, listSubUnionId4),
            /* 12 */ new SimplifiedList(22_941, listUnionId4, listSubUnionId4),
            /* 13 */ new SimplifiedList(15_400, listUnionId2),
            /* 14 */ new SimplifiedList(5_836, listUnionId1, listSubUnionId2),
            /* 15 */ new SimplifiedList(8_533),
            /* 16 */ new SimplifiedList(70_633, listUnionId5, listSubUnionId5),
            /* 17 */ new SimplifiedList(14_040, listUnionId5, listSubUnionId5),
            /* 18 */ new SimplifiedList(4_547),
            /* 19 */ new SimplifiedList(16_134, listUnionId3),
            /* 20 */ new SimplifiedList(2_329),
            /* 21 */ new SimplifiedList(6_924, listUnionId5),
            /* 22 */ new SimplifiedList(4_808),
            /* 23 */ new SimplifiedList(2_905));

        ProportionalElectionHagenbachBischoffStrategy.RecalculateNumberOfMandatesForLists(endResult);

        // validate group calculations / calculation rounds
        var rootGroup = endResult.HagenbachBischoffRootGroup!;
        rootGroup.SortCalculationRounds();
        rootGroup.Type.Should().Be(HagenbachBischoffGroupType.Root);
        rootGroup.NumberOfMandates.Should().Be(12);
        rootGroup.InitialNumberOfMandates.Should().Be(9);
        rootGroup.VoteCount.Should().Be(1_732_456);
        rootGroup.Quotient.Should().Be(1_732_456 / 13M);
        rootGroup.DistributionNumber.Should().Be(133_266);

        var rootGroupChildren = rootGroup.ChildrenOrdered.ToList();

        rootGroupChildren.Select(x => x.AllListNumbers)
            .Should()
            .Equal("01,03,04,06,14", "02,07,13", "08,19", "09,10,11,12", "16,17,21", "05", "15", "18", "20", "22", "23");

        rootGroupChildren.Select(x => x.VoteCount)
            .Should()
            .Equal(380_067, 247_629, 636_317, 345_205, 91_597, 8_519, 8_533, 4_547, 2_329, 4_808, 2_905);

        rootGroupChildren.Select(x => x.InitialNumberOfMandates)
            .Should()
            .Equal(2, 1, 4, 2, 0, 0, 0, 0, 0, 0, 0);

        rootGroupChildren.Select(x => x.NumberOfMandates)
            .Should()
            .Equal(3, 2, 5, 2, 0, 0, 0, 0, 0, 0, 0);

        var rootGroupCalculationRounds = rootGroup.CalculationRounds.ToList();
        rootGroupCalculationRounds.Should().HaveCount(3);
        rootGroupCalculationRounds[0].WinnerReason.Should().Be(HagenbachBischoffCalculationRoundWinnerReason.Quotient);
        rootGroupCalculationRounds[0].Winner.AllListNumbers.Should().Be("08,19");
        rootGroupCalculationRounds[0].GroupValues
            .Select(x => x.PreviousQuotient)
            .Should()
            .Equal(126_689m, 123_814.5m, 127_263.4m, 345_205 / 3m, 91_597m, 8_519m, 8_533m, 4547m, 2329m, 4808m, 2905m);

        rootGroupCalculationRounds[1].WinnerReason.Should().Be(HagenbachBischoffCalculationRoundWinnerReason.Quotient);
        rootGroupCalculationRounds[1].Winner.AllListNumbers.Should().Be("01,03,04,06,14");
        rootGroupCalculationRounds[1].GroupValues
            .Select(x => x.PreviousQuotient)
            .Should()
            .Equal(126_689m, 123_814.5m, 636_317 / 6m, 345_205 / 3m, 91_597m, 8_519m, 8_533m, 4547m, 2329m, 4808m, 2905m);

        rootGroupCalculationRounds[2].WinnerReason.Should().Be(HagenbachBischoffCalculationRoundWinnerReason.Quotient);
        rootGroupCalculationRounds[2].Winner.AllListNumbers.Should().Be("02,07,13");
        rootGroupCalculationRounds[2].GroupValues
            .Select(x => x.PreviousQuotient)
            .Should()
            .Equal(95_016.75m, 123_814.5m, 636_317 / 6m, 345_205 / 3m, 91_597m, 8_519m, 8_533m, 4547m, 2329m, 4808m, 2905m);

        var union1 = rootGroupChildren[0];
        union1.AllListNumbers.Should().Be("01,03,04,06,14");
        union1.NumberOfMandates.Should().Be(3);
        union1.VoteCount.Should().Be(380_067);
        union1.Quotient.Should().Be(95_016.75m);
        union1.DistributionNumber.Should().Be(95_017);
        union1.Children.Should().HaveCount(3);

        var union1Children = union1.ChildrenOrdered.ToList();
        union1Children.Select(x => x.AllListNumbers)
            .Should()
            .Equal("01,03", "06,14", "04");

        union1Children.Select(x => x.VoteCount)
            .Should()
            .Equal(286_765, 62_969, 30_333);

        union1Children.Select(x => x.InitialNumberOfMandates)
            .Should()
            .Equal(3, 0, 0);

        union1Children.Select(x => x.NumberOfMandates)
            .Should()
            .Equal(3, 0, 0);

        var subUnion1 = union1Children[0];
        subUnion1.AllListNumbers.Should().Be("01,03");
        subUnion1.NumberOfMandates.Should().Be(3);
        subUnion1.VoteCount.Should().Be(286_765);
        subUnion1.Quotient.Should().Be(71_691.25m);
        subUnion1.DistributionNumber.Should().Be(71692);
        subUnion1.Children.Should().HaveCount(2);

        var subUnion1Children = subUnion1.ChildrenOrdered.ToList();
        subUnion1Children.Select(x => x.AllListNumbers)
            .Should()
            .Equal("01", "03");

        subUnion1Children.Select(x => x.VoteCount)
            .Should()
            .Equal(26_843, 259_922);

        subUnion1Children.Select(x => x.InitialNumberOfMandates)
            .Should()
            .Equal(0, 3);

        subUnion1Children.Select(x => x.NumberOfMandates)
            .Should()
            .Equal(0, 3);

        // validate nr of mandates (end result)
        endResult.ListEndResults
            .OrderBy(x => x.List.Position)
            .Select(x => x.NumberOfMandates)
            .Should()
            .Equal(0, 0, 3, 0, 0, 0, 2, 5, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    [Fact]
    public void TestWithZeroVotes()
    {
        var endResult = GetBasicEndResult(
            5,
            new SimplifiedList(ListId1, 0, SubListUnionId1, ListUnionId1),
            new SimplifiedList(ListId2, 0, SubListUnionId1, ListUnionId1),
            new SimplifiedList(ListId3, 0, SubListUnionId2, ListUnionId1),
            new SimplifiedList(ListId4, 0, SubListUnionId2, ListUnionId1));

        ProportionalElectionHagenbachBischoffStrategy.RecalculateNumberOfMandatesForLists(endResult);

        endResult.ListEndResults.All(x => x.NumberOfMandates == 0).Should().BeTrue();
    }

    /// <summary>
    /// Don't distribute rest mandates because
    /// Art. 100 e) from <a href="https://www.gesetzessammlung.sg.ch/app/de/texts_of_law/125.3/versions/2500">here</a> is not implemented yet.
    /// </summary>
    [Fact]
    public void TestDistributeRestMandatesEdgeCaseDontDistributeRestMandates()
    {
        var endResult = GetBasicEndResult(
            2,
            new SimplifiedList(ListId1, 10000),
            new SimplifiedList(ListId2, 5000),
            new SimplifiedList(ListId3, 5000));

        ProportionalElectionHagenbachBischoffStrategy.RecalculateNumberOfMandatesForLists(endResult);

        var listEndResults = endResult.ListEndResults.ToArray();
        listEndResults[0].NumberOfMandates.Should().Be(1);
        listEndResults[1].NumberOfMandates.Should().Be(0);
        listEndResults[2].NumberOfMandates.Should().Be(0);
    }

    private ProportionalElectionEndResult GetBasicEndResult(
        int numberOfMandates,
        params SimplifiedList[] lists)
    {
        var listEndResults = new List<ProportionalElectionListEndResult>();

        var listCounter = 1;
        foreach (var list in lists)
        {
            list.Position = listCounter++;
            listEndResults.Add(BuildListEndResult(list.ListId, list.Position, list.VoteCount, list.ListUnionId, list.ListSubUnionId));
        }

        return new ProportionalElectionEndResult
        {
            ProportionalElection = new ProportionalElection
            {
                NumberOfMandates = numberOfMandates,
            },
            ListEndResults = listEndResults,
        };
    }

    private ProportionalElectionListEndResult BuildListEndResult(
        Guid listId,
        int position,
        int voteCount,
        string? listUnionKey = null,
        string? listSubUnionKey = null)
    {
        var listEndResult = new ProportionalElectionListEndResult
        {
            ListId = listId,
            List = new ProportionalElectionList
            {
                Id = listId,
                Position = position,
                OrderNumber = position.ToString("00", CultureInfo.InvariantCulture),
            },
        };

        listEndResult.List.EndResult = listEndResult;

        if (!string.IsNullOrEmpty(listUnionKey))
        {
            SetListUnion(listEndResult.List, listUnionKey, listSubUnionKey);
        }

        listEndResult.ConventionalSubTotal.UnmodifiedListBlankRowsCount = (int)(voteCount * 0.05);
        listEndResult.ConventionalSubTotal.ModifiedListBlankRowsCount = (int)(voteCount * 0.1);
        listEndResult.ConventionalSubTotal.ModifiedListVotesCount = (int)(voteCount * 0.5);
        listEndResult.ConventionalSubTotal.ListVotesCountOnOtherLists = (int)(voteCount * 0.2);
        listEndResult.ConventionalSubTotal.UnmodifiedListVotesCount = voteCount - listEndResult.ModifiedListVotesCount
                                                                                - listEndResult.BlankRowsCount;
        return listEndResult;
    }

    private void SetListUnion(
        ProportionalElectionList list,
        string listUnionKey,
        string? listSubUnionKey = null)
    {
        var listUnionId = Guid.Parse(listUnionKey);
        var listSubUnionId = string.IsNullOrEmpty(listSubUnionKey)
            ? (Guid?)null
            : Guid.Parse(listSubUnionKey);

        list.ProportionalElectionListUnionEntries.Add(new ProportionalElectionListUnionEntry
        {
            ProportionalElectionListUnion = new ProportionalElectionListUnion
            {
                Id = listUnionId,
            },
            ProportionalElectionListUnionId = listUnionId,
        });

        if (listSubUnionId == null)
        {
            return;
        }

        list.ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
            {
                new ProportionalElectionListUnionEntry
                {
                    ProportionalElectionListUnion = new ProportionalElectionListUnion
                    {
                        Id = listSubUnionId.Value,
                        ProportionalElectionRootListUnionId = listUnionId,
                    },
                    ProportionalElectionListUnionId = listSubUnionId.Value,
                },
            };
    }

    private class SimplifiedList
    {
        public SimplifiedList(int voteCount, string? listUnionId = null, string? listSubUnionId = null)
            : this(Guid.NewGuid(), voteCount, listUnionId, listSubUnionId)
        {
        }

        public SimplifiedList(string listId, int voteCount, string? listUnionId = null, string? listSubUnionId = null)
            : this(Guid.Parse(listId), voteCount, listUnionId, listSubUnionId)
        {
        }

        public SimplifiedList(
            Guid listId,
            int voteCount,
            string? listUnionId = null,
            string? listSubUnionId = null)
        {
            ListId = listId;
            VoteCount = voteCount;
            ListUnionId = listUnionId;
            ListSubUnionId = listSubUnionId;
        }

        public int Position { get; set; }

        public Guid ListId { get; set; }

        public int VoteCount { get; set; }

        public string? ListUnionId { get; set; }

        public string? ListSubUnionId { get; set; }
    }
}
