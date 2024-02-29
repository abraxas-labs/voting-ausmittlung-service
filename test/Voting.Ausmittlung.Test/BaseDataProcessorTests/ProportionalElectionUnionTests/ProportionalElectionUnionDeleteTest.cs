// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionUnionTests;

public class ProportionalElectionUnionDeleteTest : BaseDataProcessorTest
{
    public ProportionalElectionUnionDeleteTest(TestApplicationFactory factory)
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
    public async Task TestDelete()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionUnionDeleted
            {
                ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
            });
        var result = await RunOnDb(db => db.ProportionalElectionUnions.FirstOrDefaultAsync(u => u.Id == Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1)));
        result.Should().BeNull();
    }
}
