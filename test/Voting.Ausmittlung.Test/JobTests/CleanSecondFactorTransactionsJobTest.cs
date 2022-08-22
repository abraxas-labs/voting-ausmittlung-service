// (c) Copyright 2022 by Abraxas Informatik AG
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

public class CleanSecondFactorTransactionsJobTest : BaseTest<TestApplicationFactory, TestStartup>
{
    public CleanSecondFactorTransactionsJobTest(TestApplicationFactory factory)
        : base(factory)
    {
        ResetDb();
    }

    [Fact]
    public async Task ShouldWork()
    {
        await RunScoped(async (TemporaryDataContext db) =>
        {
            var secondFactorTransactionValid = new SecondFactorTransaction
            {
                Id = Guid.NewGuid(),
                ExpiredAt = MockedClock.UtcNowDate.AddMinutes(10),
            };

            var secondFactorTransactionExpired1 = new SecondFactorTransaction
            {
                Id = Guid.NewGuid(),
                ExpiredAt = MockedClock.UtcNowDate.AddMinutes(-1),
            };

            var secondFactorTransactionExpired2 = new SecondFactorTransaction
            {
                Id = Guid.NewGuid(),
                ExpiredAt = MockedClock.UtcNowDate.AddMinutes(-100),
            };

            await db.SecondFactorTransactions.AddRangeAsync(secondFactorTransactionValid, secondFactorTransactionExpired1, secondFactorTransactionExpired2);
            await db.SaveChangesAsync();
        });

        var secondFactorTransactions =
            await RunScoped<TemporaryDataContext, List<SecondFactorTransaction>>(db => db.SecondFactorTransactions.ToListAsync());

        secondFactorTransactions.Count.Should().Be(3);

        var job = GetService<CleanSecondFactorTransactionsJob>();
        await job.Run(CancellationToken.None);

        secondFactorTransactions =
            await RunScoped<TemporaryDataContext, List<SecondFactorTransaction>>(db => db.SecondFactorTransactions.ToListAsync());

        secondFactorTransactions.Count.Should().Be(1);
    }

    private void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var temporaryDb = scope.ServiceProvider.GetRequiredService<TemporaryDataContext>();
        DatabaseUtil.Truncate(db, temporaryDb);
    }
}
