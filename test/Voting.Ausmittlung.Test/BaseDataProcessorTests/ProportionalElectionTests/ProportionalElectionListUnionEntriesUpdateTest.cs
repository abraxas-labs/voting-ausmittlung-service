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

public class ProportionalElectionListUnionEntriesUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionListUnionEntriesUpdateTest(TestApplicationFactory factory)
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
    public async Task TestListUnionEntriesUpdate()
    {
        await TestEventPublisher.Publish(new ProportionalElectionListUnionEntriesUpdated
        {
            ProportionalElectionListUnionEntries = new ProportionalElectionListUnionEntriesEventData
            {
                ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
                ProportionalElectionListIds =
                    {
                        ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                        ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
                        ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                    },
            },
        });

        var idGuid = Guid.Parse(ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen);

        var union = await RunOnDb(
            db => db.ProportionalElectionListUnions
                .AsSplitQuery()
                .Include(u => u.Translations)
                .Include(x => x.ProportionalElectionListUnionEntries)
                .ThenInclude(x => x.ProportionalElectionList)
                .ThenInclude(x => x.Translations)
                .FirstAsync(x => x.Id == idGuid),
            Languages.German);

        SetDynamicIdToDefaultValue(union.Translations);
        SetDynamicIdToDefaultValue(union.ProportionalElectionListUnionEntries.SelectMany(e => e.ProportionalElectionList.Translations));
        union.MatchSnapshot();
    }
}
