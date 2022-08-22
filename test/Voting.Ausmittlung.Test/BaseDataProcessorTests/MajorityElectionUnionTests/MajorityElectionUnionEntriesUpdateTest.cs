// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionUnionTests;

public class MajorityElectionUnionEntriesUpdateTest : BaseDataProcessorTest
{
    public MajorityElectionUnionEntriesUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionUnionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestEntriesUpdate()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionUnionEntriesUpdated
            {
                MajorityElectionUnionEntries = new MajorityElectionUnionEntriesEventData
                {
                    MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
                    MajorityElectionIds =
                    {
                            MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen,
                            MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    },
                },
            });

        var electionIds = await RunOnDb(db =>
            db.MajorityElectionUnionEntries
                .Where(u => u.MajorityElectionUnionId == Guid.Parse(MajorityElectionUnionMockedData.IdStGallen1))
                .Select(u => u.MajorityElectionId)
                .OrderBy(id => id)
                .ToListAsync());

        electionIds.MatchSnapshot("electionIds");
    }
}
