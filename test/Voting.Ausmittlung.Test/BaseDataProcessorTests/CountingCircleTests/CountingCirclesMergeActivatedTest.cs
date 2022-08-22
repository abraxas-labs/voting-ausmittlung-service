// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.CountingCircleTests;

public class CountingCirclesMergeActivatedTest : CountingCircleProcessorBaseTest
{
    public CountingCirclesMergeActivatedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestActivated()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        var countingCircleId = Guid.Parse("eae2cfaf-c787-48b9-a108-c975b0a580da");

        await TestEventPublisher.Publish(
            new CountingCirclesMergerActivated
            {
                Merger = new CountingCirclesMergerEventData
                {
                    NewCountingCircle = new CountingCircleEventData
                    {
                        Name = "StGallen Aussen",
                        Bfs = "1234",
                        Id = countingCircleId.ToString(),
                        ResponsibleAuthority = new AuthorityEventData
                        {
                            Name = "StGallen Aussen",
                            Email = "stgallen@abraxas.ch",
                            Phone = "071 123 12 20",
                            Street = "WerkstrasseX",
                            City = "MyCityX",
                            Zip = "9200",
                            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                        },
                        ContactPersonSameDuringEventAsAfter = false,
                        ContactPersonDuringEvent = new ContactPersonEventData
                        {
                            Email = "stgallen@abraxas.ch",
                            Phone = "071 123 12 21",
                            MobilePhone = "071 123 12 31",
                            FamilyName = "Muster",
                            FirstName = "Hans",
                        },
                        ContactPersonAfterEvent = new ContactPersonEventData
                        {
                            Email = "stgallen@abraxas.ch",
                            Phone = "071 123 12 22",
                            MobilePhone = "071 123 12 33",
                            FamilyName = "Wichtig",
                            FirstName = "Rudolph",
                        },
                    },
                    CopyFromCountingCircleId = CountingCircleMockedData.IdStGallenHaggen,
                    MergedCountingCircleIds =
                    {
                            CountingCircleMockedData.IdStGallenHaggen,
                            CountingCircleMockedData.IdStGallenStFiden,
                    },
                },
            });

        var data = await RunOnDb(db => db.CountingCircles
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ResponsibleAuthority)
            .Include(x => x.DomainOfInfluences)
            .FirstOrDefaultAsync(x => x.Id == countingCircleId));

        data!.DomainOfInfluences.Count.Should().Be(2);
        data.DomainOfInfluences = null!;

        data.MatchSnapshot(
            x => x.ResponsibleAuthority.Id,
            x => x.ContactPersonAfterEvent!.Id,
            x => x.ContactPersonDuringEvent.Id);

        (await RunOnDb(db => db.CountingCircles
            .Where(cc => cc.Id == CountingCircleMockedData.GuidStGallenHaggen || cc.Id == CountingCircleMockedData.GuidStGallenStFiden)
            .CountAsync())).Should().Be(0);
    }

    [Fact]
    public async Task TestActivatedShouldCreateSnapshotsForContestsInTestingPhase()
    {
        await ContestMockedData.Seed(RunScoped);

        var countingCircleId = Guid.Parse("6ef0d14b-b440-4d37-be1c-d6b283998826");
        await TestEventPublisher.Publish(
            new CountingCirclesMergerActivated
            {
                Merger = new CountingCirclesMergerEventData
                {
                    NewCountingCircle = new CountingCircleEventData
                    {
                        Name = "StGallen Aussen",
                        Bfs = "1234",
                        Id = countingCircleId.ToString(),
                        ResponsibleAuthority = new AuthorityEventData
                        {
                            Name = "StGallen Aussen",
                            Email = "stgallen@abraxas.ch",
                            Phone = "071 123 12 20",
                            Street = "WerkstrasseX",
                            City = "MyCityX",
                            Zip = "9200",
                            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                        },
                        ContactPersonSameDuringEventAsAfter = false,
                        ContactPersonDuringEvent = new ContactPersonEventData
                        {
                            Email = "stgallen@abraxas.ch",
                            Phone = "071 123 12 21",
                            MobilePhone = "071 123 12 31",
                            FamilyName = "Muster",
                            FirstName = "Hans",
                        },
                        ContactPersonAfterEvent = new ContactPersonEventData
                        {
                            Email = "stgallen@abraxas.ch",
                            Phone = "071 123 12 22",
                            MobilePhone = "071 123 12 33",
                            FamilyName = "Wichtig",
                            FirstName = "Rudolph",
                        },
                    },
                    CopyFromCountingCircleId = CountingCircleMockedData.IdStGallenHaggen,
                    MergedCountingCircleIds =
                    {
                            CountingCircleMockedData.IdStGallenHaggen,
                            CountingCircleMockedData.IdStGallenStFiden,
                    },
                },
            });

        var countOfCountingCircles = await RunOnDb(db => db.CountingCircles
            .CountAsync(cc => cc.BasisCountingCircleId == countingCircleId));
        var countOfContestsInTestingPhase = await RunOnDb(db => db.Contests
            .WhereInTestingPhase()
            .CountAsync());

        var countingCircles = await RunOnDb(db => db.CountingCircles
            .Include(cc => cc.DomainOfInfluences)
            .Where(cc => cc.BasisCountingCircleId == countingCircleId)
            .ToListAsync());

        // remove non-snapshot counting circle
        var countOfCountingCircleSnapshots = countOfCountingCircles - 1;
        countOfCountingCircleSnapshots.Should().Be(countOfContestsInTestingPhase);

        countingCircles.All(cc => cc.DomainOfInfluences.Count == 2).Should().BeTrue();

        (await RunOnDb(db => db.CountingCircles
            .WhereContestIsInTestingPhase()
            .Where(cc => cc.BasisCountingCircleId == CountingCircleMockedData.GuidStGallenHaggen || cc.BasisCountingCircleId == CountingCircleMockedData.GuidStGallenStFiden)
            .CountAsync())).Should().Be(0);

        (await RunOnDb(db => db.CountingCircles
            .WhereContestIsNotInTestingPhase()
            .Where(cc => cc.BasisCountingCircleId == CountingCircleMockedData.GuidStGallenHaggen || cc.BasisCountingCircleId == CountingCircleMockedData.GuidStGallenStFiden)
            .CountAsync())).Should().NotBe(0);
    }
}
