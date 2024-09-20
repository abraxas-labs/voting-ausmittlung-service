// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Scheduler;

namespace Voting.Ausmittlung.Core.Jobs;

public class CleanSecondFactorTransactionsJob : IScheduledJob
{
    private readonly IDbRepository<TemporaryDataContext, SecondFactorTransaction> _repo;
    private readonly IClock _clock;

    public CleanSecondFactorTransactionsJob(
        IDbRepository<TemporaryDataContext, SecondFactorTransaction> repo,
        IClock clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task Run(CancellationToken ct)
    {
        var keysToDelete = await _repo
            .Query()
            .Where(x => x.ExpiredAt < _clock.UtcNow)
            .Select(x => x.Id)
            .ToListAsync(ct);
        await _repo.DeleteRangeByKey(keysToDelete);
    }
}
