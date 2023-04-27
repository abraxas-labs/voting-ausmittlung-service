// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.VotingExports.Repository;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;
using ResultExportConfigurationPoliticalBusinessMetadata = Voting.Ausmittlung.Core.Domain.ResultExportConfigurationPoliticalBusinessMetadata;

namespace Voting.Ausmittlung.Core.Services.Export;

public class ResultExportService
{
    private readonly ResultExportConfigurationRepo _resultExportConfigurationRepo;
    private readonly ExportService _exportService;
    private readonly ContestReader _contestReader;
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;
    private readonly ILogger<ResultExportService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PublisherConfig _publisherConfig;
    private readonly ContestService _contestService;
    private readonly ResultExportTemplateReader _resultExportTemplateReader;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly Dictionary<ExportProvider, IExportProviderUploader> _uploaders;

    public ResultExportService(
        ExportService exportService,
        ContestReader contestReader,
        PermissionService permissionService,
        ResultExportConfigurationRepo resultExportConfigurationRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo,
        ILogger<ResultExportService> logger,
        IServiceProvider serviceProvider,
        PublisherConfig publisherConfig,
        ContestService contestService,
        ResultExportTemplateReader resultExportTemplateReader,
        IAggregateRepository aggregateRepository,
        IEnumerable<IExportProviderUploader> uploaders)
    {
        _exportService = exportService;
        _contestReader = contestReader;
        _permissionService = permissionService;
        _resultExportConfigurationRepo = resultExportConfigurationRepo;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _publisherConfig = publisherConfig;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
        _contestService = contestService;
        _resultExportTemplateReader = resultExportTemplateReader;
        _aggregateRepository = aggregateRepository;
        _uploaders = uploaders.ToDictionary(x => x.Provider);
    }

    public async IAsyncEnumerable<FileModel> GenerateExports(
        Guid contestId,
        Guid? basisCountingCircleId,
        IReadOnlyCollection<Guid> exportTemplateIds,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var resolvedTemplates = await ResolveTemplates(contestId, basisCountingCircleId, exportTemplateIds);

        var aggregate = await _aggregateRepository.GetOrCreateById<ExportAggregate>(contestId);
        var requestId = Guid.NewGuid();

        foreach (var exportTemplate in resolvedTemplates)
        {
            var file = await _exportService.GenerateResultExport(contestId, exportTemplate, ct: ct);
            aggregate.DataExportGenerated(
                contestId,
                requestId,
                exportTemplate.Template.Key,
                exportTemplate.CountingCircleId,
                exportTemplate.PoliticalBusinessIds,
                exportTemplate.DomainOfInfluenceType ?? DomainOfInfluenceType.Unspecified);
            yield return file;
        }

        await _aggregateRepository.Save(aggregate, true);
    }

    public async Task TriggerExportsFromConfiguration(
        Guid exportConfigurationId,
        Guid contestId,
        IReadOnlyCollection<Guid> politicalBusinessIds,
        Dictionary<Guid, ResultExportConfigurationPoliticalBusinessMetadata> politicalBusinessMetadata)
    {
        _permissionService.EnsureMonitoringElectionAdmin();

        var export = await _resultExportConfigurationRepo.Query()
            .AsSplitQuery()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Contest)
            .FirstOrDefaultAsync(x => x.ExportConfigurationId == exportConfigurationId && x.ContestId == contestId)
            ?? throw new EntityNotFoundException(nameof(ResultExportConfiguration));

        _permissionService.EnsureIsDomainOfInfluenceManager(export.DomainOfInfluence);

        var politicalBusinesses = await ValidateExportConfigurationPoliticalBusinesses(contestId, politicalBusinessIds);

        export.PoliticalBusinesses = politicalBusinesses
            .Select(pb => new ResultExportConfigurationPoliticalBusiness
            {
                PoliticalBusinessId = pb.Id,
                PoliticalBusiness = pb,
                ResultExportConfigurationId = export.ExportConfigurationId,
            })
            .ToList();

        ValidateExportConfigurationPoliticalBusinessMetadata(export.Provider, politicalBusinessIds, politicalBusinessMetadata);
        export.PoliticalBusinessMetadata = new List<Data.Models.ResultExportConfigurationPoliticalBusinessMetadata>();
        foreach (var (pbId, metadata) in politicalBusinessMetadata)
        {
            export.PoliticalBusinessMetadata.Add(new Data.Models.ResultExportConfigurationPoliticalBusinessMetadata
            {
                PoliticalBusinessId = pbId,
                Token = metadata.Token,
            });
        }

        _ = GenerateExportsFromConfigurationInNewScope(export, ResultExportTriggerMode.Manual);
    }

    public async Task<FileModel> GenerateResultBundleReviewExport(Guid contestId, BundleReviewExportRequest request, CancellationToken ct = default)
    {
        request.Template = TemplateRepository.GetByKey(request.Template.Key);
        await EnsureExportBundleReviewPermissions(request.CountingCircleId, contestId);
        var file = await _exportService.GenerateBundleReviewExport(contestId, request, ct);

        var aggregate = await _aggregateRepository.GetOrCreateById<ExportAggregate>(contestId);
        aggregate.BundleReviewExported(
            contestId,
            request.Template.Key,
            request.CountingCircleId,
            request.PoliticalBusinessId,
            request.PoliticalBusinessResultBundleId);
        await _aggregateRepository.Save(aggregate, true);

        return file;
    }

    internal async Task GenerateAutomaticExportsFromConfiguration(Guid id, CancellationToken ct)
    {
        var export = await _resultExportConfigurationRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Contest)
            .Include(x => x.PoliticalBusinesses!)
                .ThenInclude(x => x.PoliticalBusiness!.DomainOfInfluence)
            .Include(x => x.PoliticalBusinesses!)
                .ThenInclude(x => x.PoliticalBusiness!.SimpleResults)
                    .ThenInclude(x => x.CountingCircle)
            .Include(x => x.PoliticalBusinessMetadata)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(nameof(ResultExportConfiguration));

        await GenerateExportsFromConfiguration(export, ResultExportTriggerMode.Automatic, ct);
    }

    /// <summary>
    /// Ensures that no duplicate political business IDs have been provided and checks the access rights.
    /// </summary>
    /// <param name="contestId">The contest ID.</param>
    /// <param name="pbIds">The political business IDs.</param>
    /// <returns>The political businesses corresponding to the IDs.</returns>
    /// <exception cref="ValidationException">Thrown if duplicate IDs have been provided or if the user does not have enough permissions.</exception>
    internal async Task<IReadOnlyCollection<SimplePoliticalBusiness>> ValidateExportConfigurationPoliticalBusinesses(Guid contestId, IReadOnlyCollection<Guid> pbIds)
    {
        var pbIdsSet = pbIds.ToHashSet();
        if (pbIdsSet.Count != pbIds.Count)
        {
            throw new ValidationException("Political business ids have to be unique");
        }

        var countingCircleIds = await _permissionService.GetReadableCountingCircleIds(contestId);
        var politicalBusinesses = await _simplePoliticalBusinessRepo.Query()
            .AsSplitQuery()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.SimpleResults)
            .ThenInclude(x => x.CountingCircle)
            .Where(x => x.Active && x.SimpleResults.Any(cc => countingCircleIds.Contains(cc.CountingCircleId)) && pbIdsSet.Contains(x.Id))
            .ToListAsync();

        pbIdsSet.ExceptWith(politicalBusinesses.Select(x => x.Id));
        if (pbIdsSet.Count > 0)
        {
            throw new ValidationException("Political business ids provided without access");
        }

        return politicalBusinesses;
    }

    internal void ValidateExportConfigurationPoliticalBusinessMetadata(
        ExportProvider provider,
        IReadOnlyCollection<Guid> pbIds,
        Dictionary<Guid, ResultExportConfigurationPoliticalBusinessMetadata> metadata)
    {
        if (provider != ExportProvider.Seantis)
        {
            return;
        }

        // For Seantis, all political businesses must have a corresponding metadata entry with a token
        foreach (var pbId in pbIds)
        {
            if (!metadata.TryGetValue(pbId, out var pbMetadata) || string.IsNullOrEmpty(pbMetadata.Token))
            {
                throw new ValidationException("Seantis configurations provided without a corresponding token");
            }
        }
    }

    internal async Task<List<ResultExportTemplate>> ResolveTemplates(
        Guid contestId,
        Guid? basisCountingCircleId,
        IReadOnlyCollection<Guid> exportTemplateIds)
    {
        var exportTemplates = await _resultExportTemplateReader.FetchExportTemplates(contestId, basisCountingCircleId);

        var idsSet = exportTemplateIds.ToHashSet();
        var resolvedTemplates = exportTemplates
            .Where(t => idsSet.Contains(t.ExportTemplateId))
            .ToList();

        if (resolvedTemplates.Count != exportTemplateIds.Count)
        {
            throw new ValidationException("Invalid export template IDs provided, could not find all matching templates");
        }

        var isMonitoringAdmin = _permissionService.IsMonitoringElectionAdmin();
        var accessiblePbIds = isMonitoringAdmin
            ? await _contestReader.GetOwnedPoliticalBusinessIds(contestId)
            : await _contestReader.GetAccessiblePoliticalBusinessIds(contestId);

        // Set the accessible political business ids when someone wants a report over the whole contest, because
        // otherwise we would have to query & filter them again later
        foreach (var resolvedTemplate in resolvedTemplates)
        {
            if (resolvedTemplate.PoliticalBusinessIds.Count == 0)
            {
                resolvedTemplate.PoliticalBusinessIds = accessiblePbIds.ToList();
            }
        }

        return resolvedTemplates;
    }

    private async Task GenerateExportsFromConfigurationInNewScope(
        ResultExportConfiguration export,
        ResultExportTriggerMode triggerMode,
        CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScopeCopyAuthAndLanguage();
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<ResultExportService>();
            await service.GenerateExportsFromConfiguration(export, triggerMode, ct);
        }
        catch (Exception ex)
        {
            // Because this method is not awaited, we need to catch all exceptions and log them here
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ResultExportService>>();
            logger.LogError(ex, "Failed to generate export in new scope");
        }
    }

    private async Task GenerateExportsFromConfiguration(ResultExportConfiguration export, ResultExportTriggerMode triggerMode, CancellationToken ct = default)
    {
        if (_publisherConfig.DisableAllExports)
        {
            _logger.LogInformation("Ignoring result export {ExportId} with {TriggerMode} since all exports are disabled", export.ExportConfigurationId, triggerMode);
            return;
        }

        var jobId = Guid.NewGuid();

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ContestId"] = export.ContestId,
            ["JobId"] = jobId,
        });

        _logger.LogInformation("working on result export {ExportId}", export.ExportConfigurationId);

        var aggregate = await _aggregateRepository.GetOrCreateById<ExportAggregate>(export.ContestId);
        aggregate.AutomatedExportTriggered(export.ContestId, export.ExportConfigurationId, jobId, triggerMode);

        var exportKeys = new List<string>();
        foreach (var key in export.ExportKeys)
        {
            if (_publisherConfig.DisabledExportTemplateKeys.Contains(key))
            {
                _logger.LogDebug("Ignoring result export {TemplateKey}, because it is disable via config", key);
                continue;
            }

            if (!TemplateRepository.TryGetByKey(key, out _))
            {
                // Templates may have been removed (by a developer), but they may still exist in an export configuration
                _logger.LogWarning("Ignoring result export {TemplateKey}, because the key was not found in the template repository", key);
                continue;
            }

            exportKeys.Add(key);
        }

        if (!_uploaders.TryGetValue(export.Provider, out var uploader))
        {
            throw new InvalidOperationException($"No export uploader for provider {export.Provider} configured");
        }

        var reportContexts = _exportService.BuildRenderContexts(exportKeys, export);
        await uploader.RenderAndUpload(
            export,
            reportContexts,
            (file, fileId) =>
            {
                aggregate.AutomatedExportGenerated(
                    export.ContestId,
                    export.ExportConfigurationId,
                    jobId,
                    file.RenderContext!.Template.Key,
                    file.RenderContext.BasisCountingCircleId,
                    file.RenderContext.PoliticalBusinessIds,
                    file.RenderContext.DomainOfInfluenceType,
                    file.Filename,
                    file.EchMessageId,
                    fileId);

                return Task.CompletedTask;
            },
            ct);

        aggregate.AutomatedExportCompleted(export.ContestId, export.ExportConfigurationId, jobId, triggerMode);
        await _aggregateRepository.Save(aggregate, true);
        _logger.LogInformation("completed result export {ExportId} ({JobId})", export.ExportConfigurationId, jobId);
    }

    private async Task EnsureExportBundleReviewPermissions(Guid countingCircleId, Guid contestId)
    {
        _permissionService.EnsureErfassungElectionAdminOrCreator();
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircle(countingCircleId, contestId);
        await _contestService.EnsureNotLocked(contestId);
    }
}
