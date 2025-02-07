// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionDeleteTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionDeleteTest(TestApplicationFactory factory)
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
    public async Task TestDelete()
    {
        var id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new SecondaryMajorityElectionDeleted { SecondaryMajorityElectionId = id });

        var idGuid = Guid.Parse(id);
        var election = await RunOnDb(db => db.SecondaryMajorityElections.FirstOrDefaultAsync(c => c.Id == idGuid));
        election.Should().BeNull();

        var simpleElection = await RunOnDb(db => db.SimplePoliticalBusinesses.FirstOrDefaultAsync(c => c.Id == idGuid));
        simpleElection.Should().BeNull();

        var primarySimpleElection = await RunOnDb(db => db.SimplePoliticalBusinesses.FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund)));
        primarySimpleElection.CountOfSecondaryBusinesses.Should().Be(2);
    }

    [Fact]
    public async Task TestDeleteOnSeparateBallot()
    {
        var id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot;
        var idGuid = Guid.Parse(id);
        var exists = await RunOnDb(db => db.MajorityElections.AnyAsync(c => c.Id == idGuid));
        exists.Should().BeTrue();

        var simpleElectionExists = await RunOnDb(db => db.SimplePoliticalBusinesses.AnyAsync(c => c.Id == idGuid));
        simpleElectionExists.Should().BeTrue();
        await TestEventPublisher.Publish(new SecondaryMajorityElectionDeleted { SecondaryMajorityElectionId = id, IsOnSeparateBallot = true });

        exists = await RunOnDb(db => db.MajorityElections.AnyAsync(c => c.Id == idGuid));
        exists.Should().BeFalse();

        simpleElectionExists = await RunOnDb(db => db.SimplePoliticalBusinesses.AnyAsync(c => c.Id == idGuid));
        simpleElectionExists.Should().BeFalse();
    }
}
