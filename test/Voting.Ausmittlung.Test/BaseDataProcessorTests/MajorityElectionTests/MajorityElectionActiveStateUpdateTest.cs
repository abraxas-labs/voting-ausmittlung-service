// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionTests;

public class MajorityElectionActiveStateUpdateTest : BaseDataProcessorTest
{
    public MajorityElectionActiveStateUpdateTest(TestApplicationFactory factory)
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
    public async Task TestActiveStateUpdate()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionActiveStateUpdated
            {
                MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen,
                Active = true,
            });
        var response = await RunOnDb(db => db.MajorityElections.FirstAsync(x =>
            x.Id == Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen)));
        response.Active.Should().BeTrue();

        var simpleResponse = await RunOnDb(db => db.SimplePoliticalBusinesses.FirstAsync(x =>
            x.Id == Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen)));
        simpleResponse.Active.Should().BeTrue();

        await TestEventPublisher.Publish(
            1,
            new MajorityElectionActiveStateUpdated
            {
                MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen,
                Active = false,
            });
        response = await RunOnDb(
            db => db.MajorityElections
                .Include(e => e.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen)),
            Languages.German);
        response.Active.Should().BeFalse();
        response.ShortDescription.Should().Be("Mw Gossau de");

        simpleResponse = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(b => b.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen)),
            Languages.Italian);
        simpleResponse.Active.Should().BeFalse();
        simpleResponse.ShortDescription.Should().Be("Mw Gossau it");
    }
}
