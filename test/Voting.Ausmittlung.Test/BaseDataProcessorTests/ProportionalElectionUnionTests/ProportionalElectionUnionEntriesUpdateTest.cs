// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionUnionTests;

public class ProportionalElectionUnionEntriesUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionUnionEntriesUpdateTest(TestApplicationFactory factory)
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
    public async Task TestEntriesUpdate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionUnionEntriesUpdated
            {
                ProportionalElectionUnionEntries = new ProportionalElectionUnionEntriesEventData
                {
                    ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
                    ProportionalElectionIds =
                    {
                            ProportionalElectionMockedData.IdUzwilProportionalElectionInContestStGallen,
                            ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                    },
                },
            });

        var electionIds = await RunOnDb(db =>
            db.ProportionalElectionUnionEntries
                .Where(u => u.ProportionalElectionUnionId == Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1))
                .Select(u => u.ProportionalElectionId)
                .OrderBy(id => id)
                .ToListAsync());

        electionIds.MatchSnapshot("electionIds");
    }
}
