// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Jobs;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.JobTests;

public class CleanExpiredProtocolExportsJobTest : BaseTest<TestApplicationFactory, TestStartup>
{
    public CleanExpiredProtocolExportsJobTest(TestApplicationFactory factory)
    : base(factory)
    {
        ResetDb();
    }

    [Fact]
    public async Task ShouldWork()
    {
        await ContestMockedData.Seed(RunScoped);

        await RunScoped(async (DataContext db) =>
        {
            var protocol1 = new ProtocolExport
            {
                Id = Guid.NewGuid(),
                Started = MockedClock.UtcNowDate.AddDays(-20),
                ContestId = ContestMockedData.GuidArchivedBundesurnengang,
            };

            var protocol2 = new ProtocolExport
            {
                Id = Guid.NewGuid(),
                Started = MockedClock.UtcNowDate.AddDays(-11),
                ContestId = ContestMockedData.GuidArchivedBundesurnengang,
            };

            var protocol3 = new ProtocolExport
            {
                Id = Guid.NewGuid(),
                Started = MockedClock.UtcNowDate.AddDays(-9),
                ContestId = ContestMockedData.GuidArchivedBundesurnengang,
            };

            db.ProtocolExports.AddRange(protocol1, protocol2, protocol3);
            await db.SaveChangesAsync();
        });

        var count = await RunScoped<DataContext, int>(db => db.ProtocolExports.CountAsync());
        count.Should().Be(3);

        var job = GetService<CleanExpiredProtocolExportsJob>();
        await job.Run(CancellationToken.None);

        count = await RunScoped<DataContext, int>(db => db.ProtocolExports.CountAsync());

        // The test config has a gap of 10 days, which means only the entry which was created 9 days ago should remain.
        count.Should().Be(1);
    }

    private void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var temporaryDb = scope.ServiceProvider.GetRequiredService<TemporaryDataContext>();
        DatabaseUtil.Truncate(db, temporaryDb);
    }
}
