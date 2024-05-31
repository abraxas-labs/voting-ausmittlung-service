// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestCreateTest : ContestProcessorBaseTest
{
    public ContestCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ExportConfigurationMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestCreated()
    {
        var id1 = Guid.Parse("0c6bcd39-89ad-4017-a924-c682b1b1237e");
        var id2 = Guid.Parse("92372a84-a9e6-4650-a906-c8455a3c04df");
        await TestEventPublisher.Publish(
            new ContestCreated
            {
                Contest = new ContestEventData
                {
                    Id = id1.ToString(),
                    Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Description = { LanguageUtil.MockAllLanguages("test") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    State = SharedProto.ContestState.TestingPhase,
                },
            },
            new ContestCreated
            {
                Contest = new ContestEventData
                {
                    Id = id2.ToString(),
                    Date = new DateTime(2020, 8, 21, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Description = { LanguageUtil.MockAllLanguages("test") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    EVoting = true,
                    EVotingFrom = new DateTime(2020, 01, 01, 4, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    EVotingTo = new DateTime(2020, 02, 01, 22, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    State = SharedProto.ContestState.TestingPhase,
                },
            });

        var data = await GetData(x => x.Id == id1 || x.Id == id2);
        SetDynamicIdToDefaultValue(data.SelectMany(x => x.Translations));
        data.Count.Should().Be(2);
        foreach (var contest in data)
        {
            contest.DomainOfInfluenceId = Guid.Empty;
            foreach (var translation in contest.Translations)
            {
                translation.Contest = null;
            }

            contest.CantonDefaults.Contest = null;
            contest.CantonDefaults.Id = Guid.Empty;

            contest.CantonDefaults.EnabledVotingCardChannels =
                contest.CantonDefaults.EnabledVotingCardChannels.OrderByPriority().ToList();

            foreach (var vcChannel in contest.CantonDefaults.EnabledVotingCardChannels)
            {
                vcChannel.Id = Guid.Empty;
            }

            foreach (var stateDescription in contest.CantonDefaults.CountingCircleResultStateDescriptions)
            {
                stateDescription.Id = Guid.Empty;
            }
        }

        data.ShouldMatchSnapshot();

        var contestDetails = await RunOnDb(db => db.ContestCountingCircleDetails
            .Where(x => x.ContestId == id1 || x.ContestId == id2)
            .ToListAsync());

        contestDetails.Should().HaveCount(2);
        var detail1 = contestDetails.First(x => x.ContestId == id1);
        detail1.EVoting.Should().BeFalse();
        var detail2 = contestDetails.First(x => x.ContestId == id2);
        detail2.EVoting.Should().BeTrue();
    }

    [Fact]
    public async Task TestSnapshotDomainOfInfluenceCreated()
    {
        var contest = new ContestEventData
        {
            Id = "9ccf9b68-94b0-4b79-be1a-09f24f467c4a",
            Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            State = SharedProto.ContestState.TestingPhase,
        };

        await TestEventPublisher.Publish(new ContestCreated { Contest = contest });

        var contestId = Guid.Parse(contest.Id);
        var createdContest = await RunOnDb(
            db => db.Contests
                .AsSplitQuery()
                .Include(c => c.DomainOfInfluence)
                .Include(c => c.DomainOfInfluenceParties)
                    .ThenInclude(p => p.Translations)
                .Include(c => c.Translations)
                .Include(c => c.CantonDefaults)
                .SingleAsync(c => c.Id == contestId),
            Languages.French);
        createdContest.DomainOfInfluenceParties.Should().HaveCount(6);
        SetDynamicIdToDefaultValue(createdContest.Translations);

        foreach (var party in createdContest.DomainOfInfluenceParties)
        {
            party.DomainOfInfluenceId = Guid.Empty;
            SetDynamicIdToDefaultValue(party.Translations);
        }

        createdContest.CantonDefaults.EnabledVotingCardChannels =
            createdContest.CantonDefaults.EnabledVotingCardChannels.OrderByPriority().ToList();

        foreach (var vcChannel in createdContest.CantonDefaults.EnabledVotingCardChannels)
        {
            vcChannel.Id = Guid.Empty;
        }

        createdContest.CantonDefaults.Id = Guid.Empty;

        foreach (var stateDescription in createdContest.CantonDefaults.CountingCircleResultStateDescriptions)
        {
            stateDescription.Id = Guid.Empty;
        }

        createdContest.DomainOfInfluenceId = Guid.Empty;
        createdContest.DomainOfInfluence.Id = Guid.Empty;
        createdContest.DomainOfInfluence.ParentId = null;
        createdContest.DomainOfInfluence.Contests = null!;
        foreach (var translation in createdContest.Translations)
        {
            translation.Contest = null;
        }

        createdContest.MatchSnapshot();
    }

    [Fact]
    public async Task TestCreatedHostedFilteredCatchUp()
    {
        var id1 = Guid.Parse("0c6bcd39-89ad-4017-a924-c682b1b1237e");
        var id2 = Guid.Parse("92372a84-a9e6-4650-a906-c8455a3c04df");
        await TestEventPublisher.Publish(
            true,
            new ContestCreated
            {
                Contest = new ContestEventData
                {
                    Id = id1.ToString(),
                    Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Description = { LanguageUtil.MockAllLanguages("test") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    State = SharedProto.ContestState.TestingPhase,
                },
            },
            new ContestCreated
            {
                Contest = new ContestEventData
                {
                    Id = id2.ToString(),
                    Date = new DateTime(2020, 8, 21, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Description = { LanguageUtil.MockAllLanguages("test") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    EVoting = true,
                    EVotingFrom = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    EVotingTo = new DateTime(2020, 02, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    State = SharedProto.ContestState.TestingPhase,
                },
            });

        var entries = ContestCache.GetAll().Where(c => (c.Id == id1 || c.Id == id2) && c.KeyData == null).ToList();
        entries.MatchSnapshot();
    }

    [Fact]
    public async Task TestResultExportConfigurationCreated()
    {
        var contest = new ContestEventData
        {
            Id = "9ccf9b68-94b0-4b79-be1a-09f24f467c4a",
            Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Description = { LanguageUtil.MockAllLanguages("test") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            State = SharedProto.ContestState.TestingPhase,
        };

        await TestEventPublisher.Publish(new ContestCreated { Contest = contest });

        var contestId = Guid.Parse(contest.Id);
        var resultExportConfigurations = await RunOnDb(
            db => db.ResultExportConfigurations
                .Where(x => x.ContestId == contestId)
                .ToListAsync());

        foreach (var resultConfig in resultExportConfigurations)
        {
            resultConfig.DomainOfInfluenceId = Guid.Empty;
        }

        resultExportConfigurations.ShouldMatchSnapshot();
    }
}
