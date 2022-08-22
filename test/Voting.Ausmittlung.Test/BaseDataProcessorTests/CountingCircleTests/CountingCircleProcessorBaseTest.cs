// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.CountingCircleTests;

public abstract class CountingCircleProcessorBaseTest : BaseDataProcessorTest
{
    protected CountingCircleProcessorBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected async Task<List<CountingCircle>> GetData(Expression<Func<CountingCircle, bool>> predicate)
    {
        return await RunOnDb(db => db.CountingCircles
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ResponsibleAuthority)
            .Where(predicate)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.SnapshotContestId)
            .ToListAsync());
    }

    protected Task<List<CountingCircle>> GetData()
        => GetData(_ => true);
}
