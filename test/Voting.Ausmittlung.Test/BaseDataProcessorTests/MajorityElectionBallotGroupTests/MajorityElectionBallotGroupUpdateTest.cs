// (c) Copyright by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupUpdateTest : BaseDataProcessorTest
{
    public MajorityElectionBallotGroupUpdateTest(TestApplicationFactory factory)
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
    public async Task TestUpdate()
    {
        await TestEventPublisher.Publish(new MajorityElectionBallotGroupUpdated
        {
            BallotGroup = new MajorityElectionBallotGroupEventData
            {
                Description = "test new",
                Position = 1,
                ShortDescription = "short - long",
                MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                Id = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                Entries =
                    {
                        new MajorityElectionBallotGroupEntryEventData
                        {
                            BlankRowCount = 1,
                            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                            Id = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
                        },
                        new MajorityElectionBallotGroupEntryEventData
                        {
                            BlankRowCount = 1,
                            ElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                            Id = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                        },
                    },
            },
        });

        var ballotGroups = await RunOnDb(db => db.MajorityElectionBallotGroups
            .Include(x => x.Entries.OrderBy(x => x.Id))
            .FirstAsync(x =>
                x.Id == Guid.Parse(MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)));
        ballotGroups.MatchSnapshot();
    }
}
