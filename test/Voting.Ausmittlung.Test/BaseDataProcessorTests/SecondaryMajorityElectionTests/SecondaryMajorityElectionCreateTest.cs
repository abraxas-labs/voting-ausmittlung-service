// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCreateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCreateTest(TestApplicationFactory factory)
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
    public async Task TestCreate()
    {
        var idElectionGroup = Guid.Parse("15a89caf-f81e-4de7-8eff-4f6524128213");
        var idElection1 = Guid.Parse("b01433b2-4cef-4b92-9f1c-0b75c63fcb7f");
        var idElection2 = Guid.Parse("798b0c48-4459-4005-8fd7-9dba3d758cb6");
        await TestEventPublisher.Publish(
            new ElectionGroupCreated
            {
                ElectionGroup = new ElectionGroupEventData
                {
                    Id = idElectionGroup.ToString(),
                    Number = 1,
                    Description = "test",
                    PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                },
            });
        await TestEventPublisher.Publish(
            1,
            new SecondaryMajorityElectionCreated
            {
                SecondaryMajorityElection = new SecondaryMajorityElectionEventData
                {
                    Id = idElection1.ToString(),
                    PoliticalBusinessNumber = "10226",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
                    Active = true,
                    NumberOfMandates = 2,
                    PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                },
            },
            new SecondaryMajorityElectionCreated
            {
                SecondaryMajorityElection = new SecondaryMajorityElectionEventData
                {
                    Id = idElection2.ToString(),
                    PoliticalBusinessNumber = "10286",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl 2") },
                    Active = false,
                    NumberOfMandates = 4,
                    PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    IsOnSeparateBallot = true,
                },
            });

        var group = await RunOnDb(
            db => db.ElectionGroups
                .OrderBy(x => x.Description)
                .FirstAsync(x => x.Id == idElectionGroup),
            Languages.German);
        group.MatchSnapshot("group");

        var elections = await RunOnDb(
            db => db.MajorityElections
                .AsSplitQuery()
                .Where(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen))
                .Include(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.Translations)
                .Include(x => x.SecondaryMajorityElectionsOnSeparateBallots)
                .ThenInclude(x => x.Translations)
                .Include(x => x.Translations)
                .OrderBy(x => x.PoliticalBusinessNumber)
                .ToListAsync(),
            Languages.German);
        RemoveDynamicData(elections);
        elections.MatchSnapshot("majorityElections");

        var simpleElections = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(x => x.Translations)
                .Where(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)
                            || x.Id == idElection1
                            || x.Id == idElection2)
                .ToListAsync(),
            Languages.German);

        RemoveDynamicData(simpleElections);
        simpleElections.MatchSnapshot("simpleElections");

        // on separate ballot should not be included
        var primarySimpleElection = await RunOnDb(db => db.SimplePoliticalBusinesses.FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)));
        primarySimpleElection.CountOfSecondaryBusinesses.Should().Be(1);
    }
}
