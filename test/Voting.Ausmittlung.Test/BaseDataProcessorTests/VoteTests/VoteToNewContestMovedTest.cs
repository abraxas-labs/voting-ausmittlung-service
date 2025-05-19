// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class VoteToNewContestMovedTest : VoteProcessorBaseTest
{
    public VoteToNewContestMovedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestToNewContestMoved()
    {
        var pbId = Guid.Parse(VoteMockedData.IdStGallenVoteInContestStGallen);
        var newContestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        await TestEventPublisher.Publish(
            new VoteToNewContestMoved
            {
                VoteId = pbId.ToString(),
                NewContestId = newContestId.ToString(),
            });

        var pb = await RunOnDb(db => db.Votes
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
