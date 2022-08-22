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

public class ProportionalElectionListReorderTest : BaseDataProcessorTest
{
    public ProportionalElectionListReorderTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestListReorder()
    {
        await TestEventPublisher.Publish(new ProportionalElectionListsReordered
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            ListOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
                            Position = 3,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                            Position = 1,
                        },
                    },
            },
        });

        var lists = await RunOnDb(
            db => db.ProportionalElectionLists
                .Include(l => l.Translations)
                .Where(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen))
                .OrderBy(x => x.Position)
                .ToListAsync(),
            Languages.Italian);

        SetDynamicIdToDefaultValue(lists.SelectMany(l => l.Translations));
        lists.MatchSnapshot();
    }
}
