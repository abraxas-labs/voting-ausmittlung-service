// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupCreateTest : BaseDataProcessorTest
{
    public MajorityElectionBallotGroupCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestCreate()
    {
        var id = Guid.Parse("7930a5eb-da4e-4689-ac78-82fc84c55a26");
        await TestEventPublisher.Publish(new MajorityElectionBallotGroupCreated
        {
            BallotGroup = new MajorityElectionBallotGroupEventData
            {
                Id = id.ToString(),
                Position = 1,
                Description = "test",
                MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                ShortDescription = "short desc",
                Entries =
                    {
                        new MajorityElectionBallotGroupEntryEventData
                        {
                            BlankRowCount = 1,
                            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                            Id = "323b8aa8-d73b-4b9d-8926-d332bd8f62d2",
                        },
                    },
            },
        });

        var group = await RunOnDb(db => db.MajorityElectionBallotGroups
            .FirstAsync(x => x.Id == id));
        group.MatchSnapshot();
    }
}
