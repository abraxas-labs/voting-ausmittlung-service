﻿// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data.Utils;
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
                    NameForProtocol = "Kanton StGallen Neu",
                    ShortName = "Sankt1",
                    Bfs = "1234",
                    Code = "C1234",
                    SortNumber = 5,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    ParentId = DomainOfInfluenceMockedData.IdBund,
                    Type = SharedProto.DomainOfInfluenceType.Ct,
                    HasForeignerVoters = true,
                    HideLowerDomainOfInfluencesInReports = true,
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
                    NameForProtocol = "Kanton StGallen Neu",
                    ShortName = "Sankt1",
                    Bfs = "1234",
                    Code = "C1234",
                    SortNumber = 5,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    ParentId = DomainOfInfluenceMockedData.IdBund,
                    Type = SharedProto.DomainOfInfluenceType.Ct,
                    HasMinorVoters = true,
                    SuperiorAuthorityDomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
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

        var doiSnapshot = await RunOnDb(db => db.DomainOfInfluences
            .Include(doi => doi.SuperiorAuthorityDomainOfInfluence)
            .SingleAsync(doi => doi.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen) && doi.SnapshotContestId == ContestMockedData.GuidBundesurnengang));

        doiSnapshot.SuperiorAuthorityDomainOfInfluence!.SnapshotContestId.Should().Be(ContestMockedData.GuidBundesurnengang);
        doiSnapshot.SuperiorAuthorityDomainOfInfluenceId.Should().Be(AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(doiSnapshot.SnapshotContestId!.Value, doiSnapshot.SuperiorAuthorityDomainOfInfluence!.BasisDomainOfInfluenceId));
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
}
