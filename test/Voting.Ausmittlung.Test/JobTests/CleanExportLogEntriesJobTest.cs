// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Jobs;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.JobTests;

public class CleanExportLogEntriesJobTest : BaseTest<TestApplicationFactory, TestStartup>
{
    public CleanExportLogEntriesJobTest(TestApplicationFactory factory)
    : base(factory)
    {
        ResetDb();
    }

    [Fact]
    public async Task ShouldWork()
    {
        await RunScoped(async (TemporaryDataContext db) =>
        {
            var logEntry1 = new ExportLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = MockedClock.UtcNowDate.AddMinutes(-20),
            };

            var logEntry2 = new ExportLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = MockedClock.UtcNowDate.AddMinutes(-11),
            };

            var logEntry3 = new ExportLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = MockedClock.UtcNowDate.AddMinutes(-9),
            };

            await db.ExportLogEntries.AddRangeAsync(logEntry1, logEntry2, logEntry3);
            await db.SaveChangesAsync();
        });

        var logEntries = await RunScoped<TemporaryDataContext, List<ExportLogEntry>>(db => db.ExportLogEntries.ToListAsync());

        logEntries.Count.Should().Be(3);

        var job = GetService<CleanExportLogEntriesJob>();
        await job.Run(CancellationToken.None);

        logEntries = await RunScoped<TemporaryDataContext, List<ExportLogEntry>>(db => db.ExportLogEntries.ToListAsync());

        // The testconfig has a gap of 10 minutes, which means only the entry which was created 9 minutes ago should remain.
        logEntries.Count.Should().Be(1);
    }

    private void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var temporaryDb = scope.ServiceProvider.GetRequiredService<TemporaryDataContext>();
        DatabaseUtil.Truncate(db, temporaryDb);
    }
}
