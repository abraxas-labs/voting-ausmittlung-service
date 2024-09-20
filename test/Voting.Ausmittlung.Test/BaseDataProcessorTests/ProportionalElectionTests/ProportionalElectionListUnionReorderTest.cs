// (c) Copyright by Abraxas Informatik AG
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

public class ProportionalElectionListUnionReorderTest : BaseDataProcessorTest
{
    public ProportionalElectionListUnionReorderTest(TestApplicationFactory factory)
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
    public async Task TestListUnionReorder()
    {
        await TestEventPublisher.Publish(new ProportionalElectionListUnionsReordered
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            ProportionalElectionListUnionOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
                            Position = 3,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListUnion3IdGossauProportionalElectionInContestStGallen,
                            Position = 1,
                        },
                    },
            },
        });

        var unions = await RunOnDb(
            db => db.ProportionalElectionListUnions
                .Include(u => u.Translations)
                .Where(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen)
                            && !x.ProportionalElectionRootListUnionId.HasValue)
                .OrderBy(x => x.Position)
                .ToListAsync(),
            Languages.French);

        SetDynamicIdToDefaultValue(unions.SelectMany(u => u.Translations));
        unions.MatchSnapshot();
    }
}
