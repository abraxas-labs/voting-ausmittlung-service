// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionListDeleteTest : BaseDataProcessorTest
{
    public ProportionalElectionListDeleteTest(TestApplicationFactory factory)
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
    public async Task TestListDelete()
    {
        await TestEventPublisher.Publish(new ProportionalElectionListDeleted
        {
            ProportionalElectionListId =
                ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
        });

        var idGuid = Guid.Parse(ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen);
        var list = await RunOnDb(db => db.ProportionalElectionLists
            .FirstOrDefaultAsync(x => x.Id == idGuid));
        list.Should().BeNull();
    }

    [Fact]
    public async Task TestListDeleteAggregatedChanges()
    {
        var id = Guid.Parse(ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen);
        await TestEventPublisher.Publish(new ProportionalElectionListDeleted { ProportionalElectionListId = id.ToString() });

        var unionLists = await RunOnDb(
            db => db.ProportionalElectionUnionLists
                .Include(l => l.Translations)
                .Where(l => Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1) == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.Translations })
                .OrderBy(x => x.OrderNumber)
                .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(unionLists.SelectMany(x => x.Translations));
        unionLists.MatchSnapshot();
    }
}
