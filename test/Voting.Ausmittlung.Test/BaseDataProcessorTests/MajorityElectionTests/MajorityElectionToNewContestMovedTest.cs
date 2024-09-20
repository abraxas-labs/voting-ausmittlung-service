﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionTests;

public class MajorityElectionToNewContestMovedTest : BaseDataProcessorTest
{
    public MajorityElectionToNewContestMovedTest(TestApplicationFactory factory)
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
    public async Task TestToNewContestMoved()
    {
        var pbId = Guid.Parse(MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen);
        var newContestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        await TestEventPublisher.Publish(
            new MajorityElectionToNewContestMoved
            {
                MajorityElectionId = pbId.ToString(),
                NewContestId = newContestId.ToString(),
            });

        var pb = await RunOnDb(db => db.MajorityElections
            .Include(x => x.Results)
            .ThenInclude(x => x.CountingCircle)
            .SingleAsync(x => x.Id == pbId));

        var simplePb = await RunOnDb(db => db.SimplePoliticalBusinesses
            .Include(x => x.SimpleResults)
            .ThenInclude(x => x.CountingCircle)
            .SingleAsync(x => x.Id == pbId));

        pb.ContestId.Should().Be(newContestId);
        pb.Results.Should().NotBeEmpty();
        pb.Results.All(x => x.CountingCircle.SnapshotContestId == newContestId).Should().BeTrue();

        simplePb.ContestId.Should().Be(newContestId);
        simplePb.SimpleResults.Should().NotBeEmpty();
        simplePb.SimpleResults.All(x => x.CountingCircle!.SnapshotContestId == newContestId).Should().BeTrue();
    }
}
