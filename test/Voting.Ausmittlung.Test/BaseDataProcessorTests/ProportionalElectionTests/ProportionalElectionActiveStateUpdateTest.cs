// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionActiveStateUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionActiveStateUpdateTest(TestApplicationFactory factory)
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
    public async Task TestActiveStateUpdate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionActiveStateUpdated
            {
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                Active = true,
            });

        var result = await RunOnDb(db =>
            db.ProportionalElections
                .FirstOrDefaultAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen)));
        result!.Active.Should().BeTrue();

        var simpleResult = await RunOnDb(db =>
            db.SimplePoliticalBusinesses
                .FirstOrDefaultAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen)));
        simpleResult!.Active.Should().BeTrue();

        await TestEventPublisher.Publish(
            1,
            new ProportionalElectionActiveStateUpdated
            {
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                Active = false,
            });

        result = await RunOnDb(
            db => db.ProportionalElections
                .Include(e => e.Translations)
                .FirstOrDefaultAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen)),
            Languages.German);
        result!.Active.Should().BeFalse();
        result.ShortDescription.Should().Be("Pw Gossau de");

        simpleResult = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(b => b.Translations)
                .FirstOrDefaultAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen)),
            Languages.Italian);
        simpleResult!.Active.Should().BeFalse();
        simpleResult.ShortDescription.Should().Be("Pw Gossau it");
    }
}
