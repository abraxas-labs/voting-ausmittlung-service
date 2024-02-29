// (c) Copyright 2024 by Abraxas Informatik AG
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

public class ProportionalElectionListUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionListUpdateTest(TestApplicationFactory factory)
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
    public async Task TestListUpdate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListUpdated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    BlankRowCount = 2,
                    OrderNumber = "o2",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated list") },
                },
            });

        var idGuid = Guid.Parse(ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen);

        var list = await RunOnDb(
            db => db.ProportionalElectionLists
                .Include(l => l.Translations)
                .FirstAsync(x => x.Id == idGuid),
            Languages.German);

        SetDynamicIdToDefaultValue(list.Translations);
        list.MatchSnapshot();
    }

    [Fact]
    public async Task TestListUpdateAggregatedChanges()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListUpdated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                    BlankRowCount = 2,
                    OrderNumber = "3",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                    Description = { LanguageUtil.MockAllLanguages("Liste 3") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Liste 3") },
                },
            });

        var unionLists = await RunOnDb(
            db => db.ProportionalElectionUnionLists
                .Where(l => Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1) == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.Translations })
                .OrderBy(x => x.OrderNumber)
                .ToListAsync(),
            Languages.German);

        var translations = unionLists.SelectMany(x => x.Translations);
        SetDynamicIdToDefaultValue(translations);
        foreach (var translation in translations)
        {
            translation.ProportionalElectionUnionListId = Guid.Empty;
        }

        unionLists.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdatedAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListAfterTestingPhaseUpdated
            {
                EventInfo = GetMockedEventInfo(),
                Id = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                ShortDescription = { LanguageUtil.MockAllLanguages("Updated list") },
                Description = { LanguageUtil.MockAllLanguages("Updated list description") },
            });

        var idGuid = Guid.Parse(ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen);

        var list = await RunOnDb(
            db => db.ProportionalElectionLists
                .Include(l => l.Translations)
                .FirstAsync(x => x.Id == idGuid),
            Languages.Romansh);

        SetDynamicIdToDefaultValue(list.Translations);
        list.MatchSnapshot();
    }
}
