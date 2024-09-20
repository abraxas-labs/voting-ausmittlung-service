// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public class DomainOfInfluenceCreateTest : DomainOfInfluenceProcessorBaseTest
{
    public DomainOfInfluenceCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestCreated()
    {
        await CantonSettingsMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(new DomainOfInfluenceCreated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = "3c3f3ae2-0439-4998-85ff-ae1f7eac94a3",
                Name = "Bezirk Uzwil",
                NameForProtocol = "Bezirk Uzwil",
                ShortName = "BZ Uz",
                Bfs = "1234",
                Code = "C1234",
                SortNumber = 3,
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                Type = SharedProto.DomainOfInfluenceType.Bz,
                Canton = SharedProto.DomainOfInfluenceCanton.Zh,
            },
        });

        var data = await GetData();
        data.MatchSnapshot();
    }

    [Fact]
    public async Task TestCreateShouldCreateSnapshotsForContestsInTestingPhase()
    {
        await ContestMockedData.Seed(RunScoped);

        var domainOfInfluenceId = Guid.Parse("2be9fdd9-d4ca-4910-aa8b-2d6cfa04cc0a");
        await TestEventPublisher.Publish(
            new DomainOfInfluenceCreated
            {
                DomainOfInfluence = new DomainOfInfluenceEventData
                {
                    Id = domainOfInfluenceId.ToString(),
                    Name = "Bezirk Uzwil",
                    NameForProtocol = "Bezirk Uzwil",
                    ShortName = "BZ Uz",
                    Bfs = "1234",
                    Code = "C1234",
                    SortNumber = 3,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    Type = SharedProto.DomainOfInfluenceType.Bz,
                },
            });

        var countOfDomainOfInfluences = await RunOnDb(db => db.DomainOfInfluences
            .CountAsync(cc => cc.BasisDomainOfInfluenceId == domainOfInfluenceId));
        var countOfContestsInTestingPhase = await RunOnDb(db => db.Contests
            .WhereInTestingPhase()
            .CountAsync());

        // remove non-snapshot domain of influence
        var countOfDomainOfInfluenceSnapshots = countOfDomainOfInfluences - 1;
        countOfDomainOfInfluenceSnapshots.Should().Be(countOfContestsInTestingPhase);
    }

    [Fact]
    public async Task TestCreateShouldCreateWithSnapshotsAndInheritedCanton()
    {
        await ContestMockedData.Seed(RunScoped);

        var domainOfInfluenceId = Guid.Parse("2be9fdd9-d4ca-4910-aa8b-2d6cfa04cc0a");
        await TestEventPublisher.Publish(
            new DomainOfInfluenceCreated
            {
                DomainOfInfluence = new DomainOfInfluenceEventData
                {
                    Id = domainOfInfluenceId.ToString(),
                    Name = "Bezirk Uzwil",
                    ShortName = "BZ Uz",
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    Type = SharedProto.DomainOfInfluenceType.Bz,
                    ParentId = DomainOfInfluenceMockedData.IdBund,
                },
            });

        var contestInTestingPhaseCantonDefaults = await RunOnDb(db => db.ContestCantonDefaults
            .AsSplitQuery()
            .Where(x => x.ContestId == Guid.Parse(ContestMockedData.IdBundesurnengang))
            .ToListAsync());

        contestInTestingPhaseCantonDefaults.Any().Should().BeTrue();
        contestInTestingPhaseCantonDefaults.All(x => x.MajorityElectionAbsoluteMajorityAlgorithm == CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo).Should().BeTrue();
        contestInTestingPhaseCantonDefaults.All(x => x.EnabledVotingCardChannels.Count == 4).Should().BeTrue();
    }
}
