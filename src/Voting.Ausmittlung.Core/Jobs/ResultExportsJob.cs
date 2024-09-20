// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Scheduler;

namespace Voting.Ausmittlung.Core.Jobs;

public class ResultExportsJob : IScheduledJob
{
    private readonly ResultExportConfigurationRepo _exportConfigurationsRepo;
    private readonly IClock _clock;
    private readonly ResultExportService _resultExportService;
    private readonly DataContext _dbContext;
    private readonly PermissionService _permissionService;
    private readonly LanguageService _languageService;
    private readonly PublisherConfig _config;
    private readonly ILogger<ResultExportsJob> _logger;

    public ResultExportsJob(
        ResultExportConfigurationRepo exportConfigurationsRepo,
        IClock clock,
        ResultExportService resultExportService,
        DataContext dbContext,
        PermissionService permissionService,
        LanguageService languageService,
        PublisherConfig config,
        ILogger<ResultExportsJob> logger)
    {
        _exportConfigurationsRepo = exportConfigurationsRepo;
        _clock = clock;
        _resultExportService = resultExportService;
        _dbContext = dbContext;
        _permissionService = permissionService;
        _languageService = languageService;
        _config = config;
        _logger = logger;
    }

    public async Task Run(CancellationToken ct)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();
        _languageService.SetLanguage(_config.AutomaticExports.Language);

        // currently we work only on one export a time
        // this should work as there should only be 1-2 automated exports per contest
        // if this ever should get larger we should reconsider introducing concurrency.
        // also with the current implementation, the exports could overlap (ex. in a cluster), but this shouldn't matter.
        while (!ct.IsCancellationRequested)
        {
            // as soon as there is no job to work on anymore
            // we wait for the next invocation of the job
            try
            {
                if (!await TryWorkOnJob(ct))
                {
                    return;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to run result exports job");
            }
        }
    }

    private async Task<bool> TryWorkOnJob(CancellationToken ct)
    {
        var exportConfigIdToWorkOn = await FetchJobAndUpdateNextExecution();
        if (exportConfigIdToWorkOn == null)
        {
            return false;
        }

        // if an execution fails, we'll just retry at the next planned execution
        await _resultExportService.GenerateAutomaticExportsFromConfiguration(exportConfigIdToWorkOn.Value, ct);
        return true;
    }

    private async Task<Guid?> FetchJobAndUpdateNextExecution()
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var now = _clock.UtcNow;
        var job = await _exportConfigurationsRepo.FetchAndLock(now);
        if (job == null)
        {
            return null;
        }

        job.UpdateNextExecution(now);
        await _exportConfigurationsRepo.Update(job);

        await transaction.CommitAsync();
        return job.Id;
    }
}
