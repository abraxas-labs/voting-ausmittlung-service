// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestUpdateTest : ContestProcessorBaseTest
{
    public ContestUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestUpdated()
    {
        var ev = NewValidUpdatedEvent();

        await RunOnDb(async db =>
        {
            var contest = await db.Contests
                .AsSplitQuery()
                .Include(x => x.CantonDefaults)
                .SingleAsync(x => x.Id == Guid.Parse(ev.Contest.Id));

            // should be preserved through updates
            contest.EVotingResultsImported = true;
            db.Update(contest);
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(ev);

        var data = await GetData(c => c.Id == Guid.Parse(ContestMockedData.IdGossau));
        data[0].EVotingResultsImported.Should().BeTrue();
        SetDynamicIdToDefaultValue(data.SelectMany(x => x.Translations));
        foreach (var contest in data)
        {
            contest.DomainOfInfluenceId = Guid.Empty;

            contest.CantonDefaults.EnabledVotingCardChannels =
                contest.CantonDefaults.EnabledVotingCardChannels.OrderByPriority().ToList();

            foreach (var vcChannel in contest.CantonDefaults.EnabledVotingCardChannels)
            {
                vcChannel.Id = Guid.Empty;
            }

            contest.CantonDefaults.Id = Guid.Empty;

            foreach (var stateDescription in contest.CantonDefaults.CountingCircleResultStateDescriptions)
            {
                stateDescription.Id = Guid.Empty;
            }
        }

        data.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdatedHostedFilteredCatchUp()
    {
        var ev = NewValidUpdatedEvent();
        await TestEventPublisher.Publish(true, ev);

        var entry = ContestCache.GetAll().Where(c => c.Id == Guid.Parse(ev.Contest.Id) && c.KeyData == null).Single();
        entry.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdateToEVotingContestShouldUpdateContestDetails()
    {
        var ev = NewValidUpdatedEvent();
        ev.Contest.EVoting = true;
        ev.Contest.EVotingFrom = MockedClock.GetDate().ToTimestamp();
        ev.Contest.EVotingTo = MockedClock.GetDate(1).ToTimestamp();

        var contestDetail = await RunOnDb(db => db.ContestCountingCircleDetails
            .Where(x => x.ContestId == Guid.Parse(ev.Contest.Id))
            .FirstAsync());

        contestDetail.EVoting.Should().BeFalse();

        await TestEventPublisher.Publish(ev);

        var updatedContestDetail = await RunOnDb(db => db.ContestCountingCircleDetails
            .Where(x => x.ContestId == Guid.Parse(ev.Contest.Id))
            .FirstAsync());

        updatedContestDetail.EVoting.Should().BeTrue();
    }

    [Fact]
    public async Task TestUpdateToNonEVotingContestShouldUpdateContestDetails()
    {
        var ev = new ContestUpdated
        {
            Contest = new ContestEventData
            {
                Id = ContestMockedData.IdUzwilEVoting,
                Date = new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test-UPDATED") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil,
                EndOfTestingPhase = new DateTime(2020, 1, 20, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                State = SharedProto.ContestState.TestingPhase,
                EVoting = false,
            },
        };

        var contestDetail = await RunOnDb(db => db.ContestCountingCircleDetails
            .Where(x => x.ContestId == Guid.Parse(ev.Contest.Id))
            .FirstAsync());

        contestDetail.EVoting.Should().BeTrue();

        await TestEventPublisher.Publish(ev);

        var updatedContestDetail = await RunOnDb(db => db.ContestCountingCircleDetails
            .Where(x => x.ContestId == Guid.Parse(ev.Contest.Id))
            .FirstAsync());

        updatedContestDetail.EVoting.Should().BeFalse();
    }

    private ContestUpdated NewValidUpdatedEvent()
    {
        return new ContestUpdated
        {
            Contest = new ContestEventData
            {
                Id = ContestMockedData.IdGossau,
                Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test-UPDATED") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                State = SharedProto.ContestState.TestingPhase,
            },
        };
    }
}
