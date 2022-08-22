// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using AutoMapper;
using EventStore.Client;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.VotingExports.Repository;
using DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Core.Services.Export;

public class ResultExportService
{
    private const string ExportsStreamNamePrefix = "voting-ausmittlung-exports-";

    private readonly ResultExportConfigurationRepo _resultExportConfigurationRepo;
    private readonly ExportService _exportService;
    private readonly ContestReader _contestReader;
    private readonly PermissionService _permissionService;
    private readonly IEventPublisher _eventPublisher;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IDokConnector _dokConnector;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;
    private readonly ILogger<ResultExportService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventSignatureService _eventSignatureService;
    private readonly PublisherConfig _publisherConfig;
    private readonly IMapper _mapper;

    public ResultExportService(
        ExportService exportService,
        ContestReader contestReader,
        IEventPublisher eventPublisher,
        EventInfoProvider eventInfoProvider,
        PermissionService permissionService,
        ResultExportConfigurationRepo resultExportConfigurationRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo,
        IDokConnector dokConnector,
        ILogger<ResultExportService> logger,
        IServiceProvider serviceProvider,
        EventSignatureService eventSignatureService,
        PublisherConfig publisherConfig,
        IMapper mapper)
    {
        _exportService = exportService;
        _contestReader = contestReader;
        _eventPublisher = eventPublisher;
        _eventInfoProvider = eventInfoProvider;
        _permissionService = permissionService;
        _resultExportConfigurationRepo = resultExportConfigurationRepo;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventSignatureService = eventSignatureService;
        _publisherConfig = publisherConfig;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
        _dokConnector = dokConnector;
        _mapper = mapper;
    }

    public async IAsyncEnumerable<FileModel> GenerateExports(
        Guid contestId,
        IReadOnlyCollection<ResultExportRequest> requests,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        InitTemplates(requests);
        EnsureAuth(requests);
        if (_publisherConfig.DisableAllExports)
        {
            throw new ValidationException("Exports are disabled");
        }

        var requestId = Guid.NewGuid();
        var isMonitoringAdmin = _permissionService.IsMonitoringElectionAdmin();
        var accessiblePbIds = isMonitoringAdmin
            ? await _contestReader.GetOwnedPoliticalBusinessIds(contestId)
            : await _contestReader.GetAccessiblePoliticalBusinessIds(contestId);

        var accessiblePbUnionIds = isMonitoringAdmin
            ? await _contestReader.GetOwnedPoliticalBusinessUnionIds(contestId)
            : ImmutableHashSet<Guid>.Empty;

        foreach (var request in requests)
        {
            if (_publisherConfig.DisabledExportTemplateKeys.Contains(request.Template.Key))
            {
                throw new ValidationException($"The export {request.Template.Key} is disabled");
            }

            if (request.PoliticalBusinessUnionId.HasValue && !accessiblePbUnionIds.Contains(request.PoliticalBusinessUnionId.Value))
            {
                throw new ForbiddenException("political business union id with missing permissions found");
            }

            if (request.PoliticalBusinessIds.Any(pbId => !accessiblePbIds.Contains(pbId)))
            {
                throw new ForbiddenException("political business id with missing permissions found");
            }

            // Set the accessible political business ids when someone wants a report over the whole contest, because
            // otherwise we would have to query & filter them again later
            if (request.PoliticalBusinessIds.Count == 0)
            {
                request.PoliticalBusinessIds = accessiblePbIds.ToList();
            }
        }

        foreach (var request in requests)
        {
            var file = await _exportService.GenerateResultExport(contestId, request, ct);
            await PublishEventsForExport(contestId, requestId, file);
            yield return file;
        }
    }

    public async Task TriggerExportsFromConfiguration(
        Guid exportConfigurationId,
        Guid contestId,
        IReadOnlyCollection<Guid> politicalBusinessIds)
    {
        _permissionService.EnsureMonitoringElectionAdmin();

        var export = await _resultExportConfigurationRepo.Query()
                         .AsSplitQuery()
                         .Include(x => x.DomainOfInfluence)
                         .Include(x => x.Contest)
                         .Include(x => x.PoliticalBusinesses!)
                         .ThenInclude(x => x.PoliticalBusiness!.DomainOfInfluence)
                         .Include(x => x.PoliticalBusinesses!)
                         .ThenInclude(x => x.PoliticalBusiness!.SimpleResults)
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

        _ = GenerateExportsFromConfigurationInNewScope(export, ResultExportTriggerMode.Manual);
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
            .Include(x => x.DomainOfInfluence)
            .Where(x => x.Active && x.SimpleResults.Any(cc => countingCircleIds.Contains(cc.CountingCircleId)) && pbIdsSet.Contains(x.Id))
            .ToListAsync();

        pbIdsSet.ExceptWith(politicalBusinesses.Select(x => x.Id));
        if (pbIdsSet.Count > 0)
        {
            throw new ValidationException("Political business ids provided without access");
        }

        return politicalBusinesses;
    }

    private async Task GenerateExportsFromConfigurationInNewScope(
        ResultExportConfiguration export,
        ResultExportTriggerMode triggerMode,
        CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScopeCopyAuthAndLanguage();
        var service = scope.ServiceProvider.GetRequiredService<ResultExportService>();
        await service.GenerateExportsFromConfiguration(export, triggerMode, ct);
    }

    private async Task GenerateExportsFromConfiguration(ResultExportConfiguration export, ResultExportTriggerMode triggerMode, CancellationToken ct = default)
    {
        if (_publisherConfig.DisableAllExports)
        {
            _logger.LogInformation("Ignoring result export {ExportId} with {TriggerMode} since all exports are disabled", export.ExportConfigurationId, triggerMode);
            return;
        }

        var jobId = Guid.NewGuid();
        _logger.LogInformation("working on result export {ExportId} ({JobId})", export.ExportConfigurationId, jobId);
        await PublishTriggeredEventForAutomatedExport(export, triggerMode, jobId);

        var exportKeys = export.ExportKeys.Where(key => !_publisherConfig.DisabledExportTemplateKeys.Contains(key));
        await foreach (var file in _exportService.GenerateResultExportsIgnoreErrors(exportKeys, export, ct))
        {
            try
            {
                var fileId = await _dokConnector.Save(export.EaiMessageType, file, ct);
                await PublishGeneratedEventForAutomatedExport(export, file, jobId, fileId);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "could not save export {FileName} {TemplateKey} {ExportId} ({JobId})",
                    file.Filename,
                    file.RenderContext.Template.Key,
                    export.ExportConfigurationId,
                    jobId);
            }
        }

        await PublishCompletedEventForAutomatedExport(export, triggerMode, jobId);
        _logger.LogInformation("completed result export {ExportId} ({JobId})", export.ExportConfigurationId, jobId);
    }

    private void EnsureAuth(IEnumerable<ResultExportRequest> requests)
    {
        var resultTypes = requests
            .Select(x => x.Template.ResultType)
            .WhereNotNull()
            .Distinct();

        foreach (var resultType in resultTypes)
        {
            _permissionService.EnsureCanExport(resultType);
        }
    }

    private void InitTemplates(IEnumerable<ResultExportRequest> requests)
    {
        foreach (var request in requests)
        {
            request.Template = TemplateRepository.GetByKey(request.Template.Key);
        }
    }

    private Task PublishGeneratedEventForAutomatedExport(
        ResultExportConfiguration config,
        FileModel exportFile,
        Guid jobId,
        string fileId)
    {
        var eventData = new ResultExportGenerated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ExportConfigurationId = config.ExportConfigurationId.ToString(),
            ContestId = config.ContestId.ToString(),
            JobId = jobId.ToString(),
            Key = exportFile.RenderContext.Template.Key,
            FileName = exportFile.Filename,
            EchMessageId = exportFile.EchMessageId ?? string.Empty,
            ConnectorFileId = fileId,
            CountingCircleId = exportFile.RenderContext.BasisCountingCircleId?.ToString() ?? string.Empty,
            PoliticalBusinessIds = { exportFile.RenderContext.PoliticalBusinessIds.Select(pb => pb.ToString()), },
            DomainOfInfluenceType = (DomainOfInfluenceType)exportFile.RenderContext.DomainOfInfluenceType,
        };
        return PublishEvent(config.ContestId, eventData);
    }

    private Task PublishTriggeredEventForAutomatedExport(ResultExportConfiguration config, ResultExportTriggerMode triggerMode, Guid jobId)
    {
        var eventData = new ResultExportTriggered
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ExportConfigurationId = config.ExportConfigurationId.ToString(),
            ContestId = config.ContestId.ToString(),
            TriggerMode = triggerMode,
            JobId = jobId.ToString(),
        };
        return PublishEvent(config.ContestId, eventData);
    }

    private Task PublishCompletedEventForAutomatedExport(ResultExportConfiguration config, ResultExportTriggerMode triggerMode, Guid jobId)
    {
        var eventData = new ResultExportCompleted
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ExportConfigurationId = config.ExportConfigurationId.ToString(),
            ContestId = config.ContestId.ToString(),
            TriggerMode = triggerMode,
            JobId = jobId.ToString(),
        };
        return PublishEvent(config.ContestId, eventData);
    }

    private Task PublishEventsForExport(Guid contestId, Guid requestId, FileModel exportFile)
    {
        var eventData = new ExportGenerated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Key = exportFile.RenderContext.Template.Key,
            ContestId = contestId.ToString(),
            RequestId = requestId.ToString(),
            CountingCircleId = exportFile.RenderContext.BasisCountingCircleId?.ToString() ?? string.Empty,
            EchMessageId = exportFile.EchMessageId ?? string.Empty,
            PoliticalBusinessIds = { exportFile.RenderContext.PoliticalBusinessIds.Select(x => x.ToString()) },
            DomainOfInfluenceType = (DomainOfInfluenceType)exportFile.RenderContext.DomainOfInfluenceType,
        };
        return PublishEvent(contestId, eventData);
    }

    private Task PublishEvent(Guid contestId, IMessage eventData)
    {
        // We don't need idempotency here, since we just "log" the export events
        // We don't care about the order of these events or whether other export events have been generated concurrently
        var streamName = ExportsStreamNamePrefix + contestId;
        var eventId = Uuid.NewUuid().ToGuid();
        var eventMetadata = _eventSignatureService.BuildEventSignatureMetadata(streamName, eventData, contestId, eventId);
        return _eventPublisher.PublishWithoutIdempotencyGuarantee(streamName, new EventWithMetadata(
            eventData,
            _mapper.Map<EventSignatureMetadata>(eventMetadata),
            eventId));
    }
}
