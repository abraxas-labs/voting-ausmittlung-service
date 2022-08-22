// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionListUnionDeleteTest : BaseDataProcessorTest
{
    public ProportionalElectionListUnionDeleteTest(TestApplicationFactory factory)
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
    public async Task TestListUnionDelete()
    {
        await TestEventPublisher.Publish(new ProportionalElectionListUnionDeleted
        {
            ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
        });

        var idGuid = Guid.Parse(ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen);

        var union = await RunOnDb(db => db.ProportionalElectionListUnions
            .FirstOrDefaultAsync(x => x.Id == idGuid));
        union.Should().BeNull();
    }
}
