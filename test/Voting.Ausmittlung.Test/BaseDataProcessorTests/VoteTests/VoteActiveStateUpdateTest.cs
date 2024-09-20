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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class VoteActiveStateUpdateTest : VoteProcessorBaseTest
{
    public VoteActiveStateUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestActiveStateUpdated()
    {
        await RunOnDb(
            async db =>
            {
                var v = await db.Votes.AsTracking().FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
                v.Active = false;

                var sv = await db.SimplePoliticalBusinesses.AsTracking().FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
                sv.Active = false;
                await db.SaveChangesAsync();
            });

        await TestEventPublisher.Publish(
            new VoteActiveStateUpdated
            {
                VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
                Active = true,
            });

        var vote = await RunOnDb(
            db => db.Votes.FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)));
        vote.Active.Should().BeTrue();

        var simpleVote = await RunOnDb(
            db => db.SimplePoliticalBusinesses.FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)));
        simpleVote.Active.Should().BeTrue();

        await TestEventPublisher.Publish(
            1,
            new VoteActiveStateUpdated
            {
                VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
                Active = false,
            });

        var voteUpdated = await RunOnDb(
            db => db.Votes
                .Include(v => v.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)),
            Languages.German);
        voteUpdated.Active.Should().BeFalse();
        voteUpdated.ShortDescription.Should().Be("Abst Gossau de");

        var simpleVoteUpdated = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(b => b.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)),
            Languages.Italian);
        simpleVoteUpdated.Active.Should().BeFalse();
        simpleVoteUpdated.ShortDescription.Should().Be("Abst Gossau it");
    }
}
