// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionListCreateTest : BaseDataProcessorTest
{
    public ProportionalElectionListCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestListCreate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListCreated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = "2e2ee2f5-3e90-47bb-bcc3-19e85dec33bf",
                    BlankRowCount = 0,
                    OrderNumber = "o1",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                    ShortDescription = { LanguageUtil.MockAllLanguages("Created list") },
                },
            },
            new ProportionalElectionListCreated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = "699f498e-2c42-465d-8b15-a6e6874e2f66",
                    BlankRowCount = 3,
                    OrderNumber = "o2",
                    Position = 2,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                    ShortDescription = { LanguageUtil.MockAllLanguages("Created list 2") },
                },
            });

        var lists = await RunOnDb(
            db => db.ProportionalElectionLists
                .Include(l => l.Translations)
                .Where(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds))
                .OrderBy(x => x.Id)
                .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(lists.SelectMany(l => l.Translations));
        lists.MatchSnapshot();
    }

    [Fact]
    public async Task TestListCreateAggregatedChanges()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListCreated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = "430f11f8-82bf-4f39-a2b9-d76e8c9dab08",
                    BlankRowCount = 0,
                    OrderNumber = "4",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                    Description = { LanguageUtil.MockAllLanguages("Liste 4") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Liste 4") },
                },
            });

        var unionLists = await RunOnDb(
            db => db.ProportionalElectionUnionLists
                .Include(l => l.Translations)
                .Where(l => Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1) == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.Translations })
                .OrderBy(d => d.OrderNumber)
                .ToListAsync(),
            Languages.Romansh);

        var translations = unionLists.SelectMany(x => x.Translations);
        SetDynamicIdToDefaultValue(translations);
        foreach (var translation in translations)
        {
            translation.ProportionalElectionUnionListId = Guid.Empty;
        }

        unionLists.MatchSnapshot();
    }
}
