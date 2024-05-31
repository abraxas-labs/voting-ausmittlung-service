// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.TemporaryData.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Scheduler;

namespace Voting.Ausmittlung.Core.Jobs;

public class CleanExportLogEntriesJob : IScheduledJob
{
    private readonly ExportLogEntryRepo _repo;
    private readonly IClock _clock;
    private readonly TimeSpan _cleanUpGap;

    public CleanExportLogEntriesJob(
        ExportLogEntryRepo repo,
        IClock clock,
        PublisherConfig config)
    {
        _repo = repo;
        _clock = clock;
        _cleanUpGap = config.ExportRateLimit.CleanUpGap;
    }

    public async Task Run(CancellationToken ct)
    {
        var deleteBeforeTimestamp = _clock.UtcNow.Subtract(_cleanUpGap);

        await _repo
            .Query()
            .Where(x => x.Timestamp < deleteBeforeTimestamp)
            .ExecuteDeleteAsync();
    }
}
