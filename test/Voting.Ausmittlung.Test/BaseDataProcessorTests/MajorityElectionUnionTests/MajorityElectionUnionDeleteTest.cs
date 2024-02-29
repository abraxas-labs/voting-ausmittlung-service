// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionUnionTests;

public class MajorityElectionUnionDeleteTest : BaseDataProcessorTest
{
    public MajorityElectionUnionDeleteTest(TestApplicationFactory factory)
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
    public async Task TestDelete()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionUnionDeleted
            {
                MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
            });
        var result = await RunOnDb(db => db.MajorityElectionUnions.FirstOrDefaultAsync(u => u.Id == Guid.Parse(MajorityElectionUnionMockedData.IdStGallen1)));
        result.Should().BeNull();
    }
}
