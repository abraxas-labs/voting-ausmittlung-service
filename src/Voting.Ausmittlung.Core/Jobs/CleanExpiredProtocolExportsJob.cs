// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Scheduler;

namespace Voting.Ausmittlung.Core.Jobs;

public class CleanExpiredProtocolExportsJob : IScheduledJob
{
    private readonly IDbRepository<DataContext, ProtocolExport> _repo;
    private readonly IClock _clock;
    private readonly PublisherConfig _config;

    public CleanExpiredProtocolExportsJob(
        IDbRepository<DataContext, ProtocolExport> repo,
        IClock clock,
        PublisherConfig config)
    {
        _repo = repo;
        _clock = clock;
        _config = config;
    }

    public async Task Run(CancellationToken ct)
    {
        var deletionDate = _clock.UtcNow.AddDays(-_config.ProtocolExportExpiryInDays);

        await _repo
            .Query()
            .Where(x => x.Started < deletionDate)
            .ExecuteDeleteAsync(ct);
    }
}
