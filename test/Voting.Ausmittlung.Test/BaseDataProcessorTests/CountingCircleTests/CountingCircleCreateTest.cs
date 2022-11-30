// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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

public class CountingCircleCreateTest : CountingCircleProcessorBaseTest
{
    public CountingCircleCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestCreate()
    {
        await TestEventPublisher.Publish(
            new CountingCircleCreated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Name = "Uzwil",
                    NameForProtocol = "Stadt Uzwil",
                    Bfs = "1234",
                    SortNumber = 5000,
                    Id = "eae2cfaf-c787-48b9-a108-c975b0a580da",
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "Uzwil",
                        Email = "uzwil-test@abraxas.ch",
                        Phone = "071 123 12 20",
                        Street = "WerkstrasseX",
                        City = "MyCityX",
                        Zip = "9200",
                        SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test@abraxas.ch",
                        Phone = "071 123 12 21",
                        MobilePhone = "071 123 12 31",
                        FamilyName = "Muster",
                        FirstName = "Hans",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test2@abraxas.ch",
                        Phone = "071 123 12 22",
                        MobilePhone = "071 123 12 33",
                        FamilyName = "Wichtig",
                        FirstName = "Rudolph",
                    },
                },
            },
            new CountingCircleCreated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Name = "St. Gallen",
                    NameForProtocol = "Stadt St. Gallen",
                    Bfs = "5500",
                    Code = "C5500",
                    Id = "eae2cfaf-c787-48b9-a108-c975b0a580db",
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "St. Gallen",
                        Email = "sg@abraxas.ch",
                        Phone = "071 123 12 20",
                        Street = "WerkstrasseSG",
                        City = "MyCitySG",
                        Zip = "9000",
                        SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "sg@abraxas.ch",
                        Phone = "071 123 12 21",
                        MobilePhone = "071 123 12 31",
                        FamilyName = "Muster-sg",
                        FirstName = "Hans-sg",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "sg@abraxas.ch",
                        Phone = "071 123 12 22",
                        MobilePhone = "071 123 12 33",
                        FamilyName = "Wichtig-sg",
                        FirstName = "Rudolph-sg",
                    },
                },
            });

        var data = await GetData();
        data.MatchSnapshot(
            x => x.ResponsibleAuthority!.Id,
            x => x.ContactPersonAfterEvent!.Id,
            x => x.ContactPersonDuringEvent!.Id);
    }

    [Fact]
    public async Task TestCreateShouldCreateSnapshotsForContestsInTestingPhase()
    {
        await ContestMockedData.Seed(RunScoped);

        var countingCircleId = Guid.Parse("6ef0d14b-b440-4d37-be1c-d6b283998826");
        await TestEventPublisher.Publish(
            new CountingCircleCreated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Name = "Test",
                    NameForProtocol = "Test",
                    Bfs = "12384",
                    Code = "C12384",
                    Id = countingCircleId.ToString(),
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "Test",
                        Email = "uzwil-test@abraxas.ch",
                        Phone = "071 123 12 20",
                        Street = "WerkstrasseX",
                        City = "MyCityX",
                        Zip = "9200",
                        SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test@abraxas.ch",
                        Phone = "071 123 12 21",
                        MobilePhone = "071 123 12 31",
                        FamilyName = "Muster",
                        FirstName = "Hans",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test2@abraxas.ch",
                        Phone = "071 123 12 22",
                        MobilePhone = "071 123 12 33",
                        FamilyName = "Wichtig",
                        FirstName = "Rudolph",
                    },
                },
            });

        var countOfCountingCircles = await RunOnDb(db => db.CountingCircles
            .CountAsync(cc => cc.BasisCountingCircleId == countingCircleId));
        var countOfContestsInTestingPhase = await RunOnDb(db => db.Contests
            .WhereInTestingPhase()
            .CountAsync());

        // remove non-snapshot counting circle
        var countOfCountingCircleSnapshots = countOfCountingCircles - 1;
        countOfCountingCircleSnapshots.Should().Be(countOfContestsInTestingPhase);
    }
}
