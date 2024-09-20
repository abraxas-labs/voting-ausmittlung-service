// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.CountingCircleTests;

public class CountingCircleDeleteTest : CountingCircleProcessorBaseTest
{
    public CountingCircleDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    [Fact]
    public async Task TestDelete()
    {
        await CountingCircleMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new CountingCircleDeleted
            {
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            },
            new CountingCircleDeleted
            {
                CountingCircleId = CountingCircleMockedData.IdGossau,
            },
            new CountingCircleDeleted
            {
                CountingCircleId = CountingCircleMockedData.IdStGallen,
            },
            new CountingCircleDeleted
            {
                CountingCircleId = CountingCircleMockedData.IdRorschach,
            });
        var data = await GetData();
        data.MatchSnapshot(
            x => x.ResponsibleAuthority!.Id,
            x => x.ContactPersonAfterEvent!.Id,
            x => x.ContactPersonDuringEvent!.Id);
    }

    [Fact]
    public async Task TestDeleteShouldDeleteSnapshotsForContestsInTestingPhase()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new CountingCircleDeleted
            {
                CountingCircleId = CountingCircleMockedData.IdGossau,
            });

        var countingCircleId = CountingCircleMockedData.GuidGossau;
        var countOfCountingCircles = await RunOnDb(db => db.CountingCircles
            .WhereContestIsInTestingPhase()
            .CountAsync(cc => cc.BasisCountingCircleId == countingCircleId));

        countOfCountingCircles.Should().Be(0);
    }
}
