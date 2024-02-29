// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionActiveStateUpdateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionActiveStateUpdateTest(TestApplicationFactory factory)
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
    public async Task TestUpdateActiveState()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionActiveStateUpdated
            {
                SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                Active = true,
            });

        var result = await RunOnDb(db => db.SecondaryMajorityElections
            .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund)));
        result.Active.Should().BeTrue();

        var simpleResult = await RunOnDb(db => db.SimplePoliticalBusinesses
            .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund)));
        simpleResult.Active.Should().BeTrue();

        await TestEventPublisher.Publish(
            1,
            new SecondaryMajorityElectionActiveStateUpdated
            {
                SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                Active = false,
            });

        result = await RunOnDb(
            db => db.SecondaryMajorityElections
                .Include(e => e.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund)),
            Languages.German);
        result.Active.Should().BeFalse();
        result.ShortDescription.Should().Be("short de");

        simpleResult = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(b => b.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund)),
            Languages.Italian);
        simpleResult.Active.Should().BeFalse();
        simpleResult.ShortDescription.Should().Be("short it");
    }
}
