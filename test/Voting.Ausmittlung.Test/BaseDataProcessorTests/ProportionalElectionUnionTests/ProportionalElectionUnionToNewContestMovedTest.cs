// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionUnionTests;

public class ProportionalElectionUnionToNewContestMovedTest : BaseDataProcessorTest
{
    public ProportionalElectionUnionToNewContestMovedTest(TestApplicationFactory factory)
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
    public async Task TestToNewContestMoved()
    {
        var pbUnionId = Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1);
        var newContestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        await TestEventPublisher.Publish(
            new ProportionalElectionUnionToNewContestMoved
            {
                ProportionalElectionUnionId = pbUnionId.ToString(),
                NewContestId = newContestId.ToString(),
            });

        var result = await RunOnDb(db => db.ProportionalElectionUnions.SingleAsync(u => u.Id == pbUnionId));
        result.ContestId.Should().Be(newContestId);
    }
}
