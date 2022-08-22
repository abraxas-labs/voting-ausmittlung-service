// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdateTest : DomainOfInfluenceProcessorBaseTest
{
    public DomainOfInfluenceUpdateTest(TestApplicationFactory factory)
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
        await DomainOfInfluenceMockedData.Seed(RunScoped);
        await CantonSettingsMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new DomainOfInfluenceUpdated
            {
                DomainOfInfluence = new DomainOfInfluenceEventData
                {
                    Id = DomainOfInfluenceMockedData.IdStGallen,
                    Name = "St. Gallen Neu",
                    ShortName = "Sankt1",
                    Bfs = "1234",
                    Code = "C1234",
                    SortNumber = 5,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    ParentId = DomainOfInfluenceMockedData.IdBund,
                    Type = SharedProto.DomainOfInfluenceType.Ct,
                },
            });

        var data = await GetData();
        data.MatchSnapshot();
    }

    [Fact]
    public async Task TestSnapshotsUpdated()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new DomainOfInfluenceUpdated
            {
                DomainOfInfluence = new DomainOfInfluenceEventData
                {
                    Id = DomainOfInfluenceMockedData.IdStGallen,
                    Name = "St. Gallen Neu",
                    ShortName = "Sankt1",
                    Bfs = "1234",
                    Code = "C1234",
                    SortNumber = 5,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    ParentId = DomainOfInfluenceMockedData.IdBund,
                    Type = SharedProto.DomainOfInfluenceType.Ct,
                },
            });

        var data = await GetData(doi => doi.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen));
        foreach (var domainOfInfluence in data)
        {
            domainOfInfluence.CountingCircles = new List<DomainOfInfluenceCountingCircle>();
        }

        data.MatchSnapshot(
            x => x.Id,
            x => x.ParentId!);
    }

    [Fact]
    public async Task TestUpdateInheritedCantons()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new DomainOfInfluenceUpdated
            {
                DomainOfInfluence = new DomainOfInfluenceEventData
                {
                    Id = DomainOfInfluenceMockedData.IdBund,
                    Name = "Bund Neu",
                    ShortName = "Bund1",
                    SecureConnectId = "100",
                    Type = SharedProto.DomainOfInfluenceType.Ch,
                    Canton = SharedProto.DomainOfInfluenceCanton.Tg,
                },
            });

        var domainOfInfluence = await RunOnDb(db => db.DomainOfInfluences
                .AsSplitQuery()
                .Include(doi => doi.Children).ThenInclude(doi => doi.Children)
                .FirstOrDefaultAsync(doi => doi.Id == Guid.Parse(DomainOfInfluenceMockedData.IdBund)));
        var childDomainOfInfluences = domainOfInfluence!.Children.Concat(domainOfInfluence.Children.SelectMany(doi => doi.Children));

        childDomainOfInfluences.All(doi => doi.Canton == DomainOfInfluenceCanton.Tg).Should().BeTrue();
    }

    [Fact]
    public async Task TestUpdateRootDoiCanton()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new DomainOfInfluenceUpdated
            {
                DomainOfInfluence = new DomainOfInfluenceEventData
                {
                    Id = DomainOfInfluenceMockedData.IdBund,
                    Name = "Bund Neu",
                    ShortName = "Bund1",
                    SecureConnectId = "100",
                    Type = SharedProto.DomainOfInfluenceType.Ch,
                    Canton = SharedProto.DomainOfInfluenceCanton.Zh,
                },
            });

        var data = await GetData(doi => doi.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdBund));

        var contestPastDois = data.Where(doi => doi.SnapshotContestId == Guid.Parse(ContestMockedData.IdVergangenerBundesurnengang))
            .ToList();
        var contestInTestingPhaseDois = data.Where(doi => doi.SnapshotContestId == Guid.Parse(ContestMockedData.IdBundesurnengang))
            .ToList();
        var baseDois = data.Where(doi => doi.SnapshotContestId == null)
            .ToList();

        contestPastDois.Any().Should().BeTrue();
        contestInTestingPhaseDois.Any().Should().BeTrue();
        baseDois.Any().Should().BeTrue();

        // only base dois and dois in testing phase should be affected
        contestPastDois.All(doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms.Count == 3).Should().BeFalse();
        contestInTestingPhaseDois.All(doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms.Count == 3).Should().BeTrue();
        baseDois.All(doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms.Count == 3).Should().BeTrue();
    }
}
