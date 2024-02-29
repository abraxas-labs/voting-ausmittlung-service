// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class CountOfVotersInformationSubTotalRepo : DbRepository<DataContext, CountOfVotersInformationSubTotal>
{
    public CountOfVotersInformationSubTotalRepo(DataContext context)
        : base(context)
    {
    }

    public Task<Dictionary<Guid, int>> GetCountOfVotersByCountCircleId(Guid contestId, VoterType voterType, CancellationToken ct = default)
    {
        return Query()
            .Where(x => x.VoterType == voterType && x.ContestCountingCircleDetails.ContestId == contestId)
            .GroupBy(x => x.ContestCountingCircleDetails.CountingCircleId)
            .Select(x => new
            {
                x.Key,
                Sum = x.Sum(z => z.CountOfVoters.GetValueOrDefault()),
            })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);
    }
}
