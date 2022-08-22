// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupDeleteTest : BaseDataProcessorTest
{
    public MajorityElectionBallotGroupDeleteTest(TestApplicationFactory factory)
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
    public async Task TestAggregate()
    {
        var id = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new MajorityElectionBallotGroupDeleted { BallotGroupId = id });

        var idGuid = Guid.Parse(id);
        var group = await RunOnDb(db => db.MajorityElectionBallotGroups.FirstOrDefaultAsync(c => c.Id == idGuid));
        group.Should().BeNull();
    }
}
