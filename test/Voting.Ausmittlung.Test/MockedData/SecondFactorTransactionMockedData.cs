// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Ausmittlung.Test.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.MockedData;

public static class SecondFactorTransactionMockedData
{
    public static readonly string ExternalIdSecondFactorTransaction = SecureConnectTestDefaults.MockedVerified2faId;

    public static SecondFactorTransaction SecondFactorTransaction
        => new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = new DateTime(2020, 1, 9, 0, 0, 0, DateTimeKind.Utc),
            LastUpdatedAt = new DateTime(2020, 1, 9, 0, 0, 0, DateTimeKind.Utc),
            ExpiredAt = new DateTime(2020, 1, 9, 0, 10, 0, DateTimeKind.Utc),
            ExternalIdentifier = SecureConnectTestDefaults.MockedVerified2faId,
            PollCount = 0,
            UserId = TestDefaults.UserId,
            ActionId = ActionIdComparerMock.ActionIdHashMock,
        };

    public static IEnumerable<SecondFactorTransaction> All
    {
        get
        {
            yield return SecondFactorTransaction;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        var secondFactorTransactions = All.ToList();
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<TemporaryDataContext>();
            db.SecondFactorTransactions.AddRange(secondFactorTransactions);
            await db.SaveChangesAsync();
        });
    }
}
