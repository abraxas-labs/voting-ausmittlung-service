// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.CountingCircleTests;

public class CountingCircleUpdateTest : CountingCircleProcessorBaseTest
{
    public CountingCircleUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    [Fact]
    public async Task TestUpdated()
    {
        await CountingCircleMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new CountingCircleUpdated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Id = CountingCircleMockedData.IdUzwil,
                    Name = "Uzwil-2",
                    NameForProtocol = "Stadt Uzwil-2",
                    Bfs = "1234-2",
                    Code = "C1234-2",
                    SortNumber = 9999,
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "Uzwil-2",
                        Email = "uzwil-test2@abraxas.ch",
                        Phone = "072 123 12 20",
                        Street = "WerkstrasseX",
                        City = "MyCityX",
                        Zip = "9200",
                        SecureConnectId = TestDefaults.TenantId,
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test-2@abraxas.ch",
                        Phone = "072 123 12 21",
                        MobilePhone = "072 123 12 31",
                        FamilyName = "Muster-2",
                        FirstName = "Hans-2",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test22@abraxas.ch",
                        Phone = "072 123 12 22",
                        MobilePhone = "072 123 12 33",
                        FamilyName = "Wichtig-2",
                        FirstName = "Rudolph-2",
                    },
                },
            },
            new CountingCircleUpdated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Id = CountingCircleMockedData.IdStGallen,
                    Name = "St. Gallen-2",
                    Bfs = "55002",
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "St. Gallen2",
                        Email = "sg2@abraxas.ch",
                        Phone = "072 123 12 20",
                        Street = "WerkstrasseSG",
                        City = "MyCitysg",
                        Zip = "9000",
                        SecureConnectId = "123444",
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "sg2@abraxas.ch",
                        Phone = "072 123 12 21",
                        MobilePhone = "072 123 12 31",
                        FamilyName = "Muster-sg2",
                        FirstName = "Hans-sg2",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "sg2@abraxas.ch",
                        Phone = "072 123 12 22",
                        MobilePhone = "072 123 12 33",
                        FamilyName = "Wichtig-sg2",
                        FirstName = "Rudolph-sg2",
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
    public async Task TestSnapshotsUpdated()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new CountingCircleUpdated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Id = CountingCircleMockedData.IdUzwil,
                    Name = "Uzwil-2",
                    NameForProtocol = "Stadt Uzwil-2",
                    Bfs = "1234-2",
                    Code = "C1234-2",
                    SortNumber = 9999,
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "Uzwil-2",
                        Email = "uzwil-test2@abraxas.ch",
                        Phone = "072 123 12 20",
                        Street = "WerkstrasseX",
                        City = "MyCityX",
                        Zip = "9200",
                        SecureConnectId = TestDefaults.TenantId,
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test-2@abraxas.ch",
                        Phone = "072 123 12 21",
                        MobilePhone = "072 123 12 31",
                        FamilyName = "Muster-2",
                        FirstName = "Hans-2",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test22@abraxas.ch",
                        Phone = "072 123 12 22",
                        MobilePhone = "072 123 12 33",
                        FamilyName = "Wichtig-2",
                        FirstName = "Rudolph-2",
                    },
                    Electorates =
                    {
                        new CountingCircleElectorateEventData
                        {
                            Id = "f705ac9f-e9f6-47c4-bfb7-33ed9b22c6d4",
                            DomainOfInfluenceTypes =
                            {
                                DomainOfInfluenceType.Ch,
                                DomainOfInfluenceType.Ct,
                                DomainOfInfluenceType.Bz,
                                DomainOfInfluenceType.Mu,
                                DomainOfInfluenceType.Sk,
                                DomainOfInfluenceType.An,
                            },
                        },
                        new CountingCircleElectorateEventData
                        {
                            Id = "d7994447-f9bf-4334-a0cb-c7c0218f2068",
                            DomainOfInfluenceTypes =
                            {
                                DomainOfInfluenceType.Og,
                            },
                        },
                    },
                },
            });

        var data = await GetData(cc => cc.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil);
        data.MatchSnapshot(
            x => x.Id,
            x => x.ResponsibleAuthority!.Id,
            x => x.ResponsibleAuthority!.CountingCircleId,
            x => x.ContactPersonAfterEvent!.Id,
            x => x.ContactPersonAfterEvent!.CountingCircleAfterEventId!,
            x => x.ContactPersonDuringEvent!.Id,
            x => x.ContactPersonDuringEvent!.CountingCircleDuringEventId!);
    }
}
