// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Cryptography.Asymmetric;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestProcessorBaseTest : BaseDataProcessorTest
{
    protected ContestProcessorBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    protected async Task<List<Contest>> GetData(
        Expression<Func<Contest, bool>> predicate,
        Func<IQueryable<Contest>, IQueryable<Contest>>? queryBuilder = null)
    {
        return await RunOnDb(
            db =>
            {
                var q = db.Contests.AsSplitQuery().Where(predicate);
                if (queryBuilder != null)
                {
                    q = queryBuilder(q);
                }

                return q
                    .OrderBy(x => x.Date)
                    .Include(x => x.Translations)
                    .Include(x => x.CantonDefaults)
                    .ToListAsync();
            },
            Languages.German);
    }

    protected void SetContestCacheKey(Guid contestId, EcdsaPrivateKey? key)
    {
        var entry = ContestCache.Get(contestId);
        entry.KeyData = key != null
            ? new ContestCacheEntryKeyData(key, entry.Date, entry.PastLockedPer)
            : null;
    }

    protected Task<List<Contest>> GetData()
        => GetData(_ => true);
}
