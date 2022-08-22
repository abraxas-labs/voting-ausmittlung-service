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

public class ProportionalElectionCandidateReorderTest : BaseDataProcessorTest
{
    public ProportionalElectionCandidateReorderTest(TestApplicationFactory factory)
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
    public async Task TestCandidateReorder()
    {
        await TestEventPublisher.Publish(new ProportionalElectionCandidatesReordered
        {
            ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
            CandidateOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestStGallen,
                            Position = 1,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                            Position = 3,
                        },
                    },
            },
        });

        var listId = Guid.Parse(ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen);
        var candidates = await RunOnDb(
            db => db.ProportionalElectionCandidates
                .AsSplitQuery()
                .Include(c => c.Translations)
                .Include(x => x.ProportionalElectionList)
                .ThenInclude(l => l.Translations)
                .Where(x => x.ProportionalElectionListId == listId)
                .OrderBy(x => x.Position)
                .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(candidates.SelectMany(c => c.Translations));
        SetDynamicIdToDefaultValue(candidates.SelectMany(c => c.ProportionalElectionList.Translations));
        candidates.MatchSnapshot();
    }
}
