// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services;
using Voting.Lib.Database.Repositories;
using Voting.Lib.DmDoc;
using Voting.Lib.DmDoc.Models;
using Voting.Lib.DmDoc.Serialization.Json;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Core.Services.Export;

public class ProtocolExportService
{
    private readonly ExportService _exportService;
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepo;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ResultExportService _resultExportService;
    private readonly IPdfService _pdfService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly PublisherConfig _publisherConfig;
    private readonly IDmDocDraftCleanupQueue _draftCleanupQueue;
    private readonly ILogger<ProtocolExportService> _logger;
    private readonly ExportRateLimitService _rateLimitService;
    private readonly IAuth _auth;

    public ProtocolExportService(
        ExportService exportService,
        PermissionService permissionService,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        IAggregateRepository aggregateRepository,
        ResultExportService resultExportService,
        IPdfService pdfService,
        IDbRepository<DataContext, Contest> contestRepo,
        PublisherConfig publisherConfig,
        IDmDocDraftCleanupQueue draftCleanupQueue,
        ILogger<ProtocolExportService> logger,
        ExportRateLimitService rateLimitService,
        IAuth auth)
    {
        _exportService = exportService;
        _permissionService = permissionService;
        _protocolExportRepo = protocolExportRepo;
        _aggregateRepository = aggregateRepository;
        _resultExportService = resultExportService;
        _pdfService = pdfService;
        _contestRepo = contestRepo;
        _publisherConfig = publisherConfig;
        _draftCleanupQueue = draftCleanupQueue;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _auth = auth;
    }

    public async IAsyncEnumerable<FileModel> GetProtocolExports(
        Guid contestId,
        Guid? basisCountingCircleId,
        IReadOnlyCollection<Guid> protocolExportIds,
        bool isBundleReview,
        bool accessiblePbs = false,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var protocolExports = await _protocolExportRepo
            .Query()
            .Where(x => protocolExportIds.Contains(x.Id) && x.ContestId == contestId)
            .ToListAsync(ct);

        if (protocolExports.Count != protocolExportIds.Count || protocolExports.Any(x => x.State != ProtocolExportState.Completed))
        {
            throw new ValidationException("Couldn't find all protocol exports");
        }

        var exportTemplateIds = protocolExports.ConvertAll(e => e.ExportTemplateId);

        // This ensures that the user has access to the generated protocols.
        // Bundle review templates cannot be resolved since they aren't listed in the template repository, so we bypass them.
        if (!isBundleReview)
        {
            await _resultExportService.ResolveTemplates(contestId, basisCountingCircleId, exportTemplateIds, accessiblePbs);
        }

        foreach (var protocolExport in protocolExports)
        {
            var pdfStream = await _pdfService.GetPdf(protocolExport.PrintJobId, ct);
            yield return new FileModel(null, protocolExport.FileName, ExportFileFormat.Pdf, async (w, ct) =>
            {
                try
                {
                    await pdfStream.CopyToAsync(w.AsStream(), ct);
                }
                finally
                {
                    await pdfStream.DisposeAsync();
                }
            });
        }
    }

    public async Task<IReadOnlyCollection<Guid>> StartExports(
        Guid contestId,
        Guid? basisCountingCircleId,
        IReadOnlyCollection<Guid> exportTemplateIds,
        bool internalRateLimit,
        bool accessiblePbs = false,
        CancellationToken ct = default)
    {
        var exportTemplates = await _resultExportService.ResolveTemplates(contestId, basisCountingCircleId, exportTemplateIds, accessiblePbs);

        if (internalRateLimit)
        {
            await _rateLimitService.CheckAndLog(exportTemplates);
        }

        var contest = await _contestRepo.Query()
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefaultAsync(x => x.Id == contestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), contestId);

        var viewablePartialResultsCountingCircleIds = await _permissionService.GetViewablePartialResultsCountingCircleIds(contestId, ct);
        var requestId = Guid.NewGuid();
        var protocolExportIds = new HashSet<Guid>();

        foreach (var exportTemplate in exportTemplates)
        {
            var protocolExportId = AusmittlungUuidV5.BuildProtocolExport(
                contestId,
                contest.State.TestingPhaseEnded(),
                exportTemplate.ExportTemplateId);
            protocolExportIds.Add(protocolExportId);

            var aggregate = await _aggregateRepository.GetOrCreateById<ProtocolExportAggregate>(protocolExportId);

            var callbackToken = Guid.NewGuid().ToString();
            var asyncPdfGenerationInfo = new AsyncPdfGenerationInfo
            {
                WebhookUrl = _publisherConfig.Documatrix.GetProtocolExportCallbackUrl(protocolExportId, callbackToken),
            };

            var exportTemplateKeyCantonSuffix = _publisherConfig.ExportTemplateKeyCantonSuffixEnabled
                ? $"_{contest.DomainOfInfluence.Canton.ToString().ToLower(CultureInfo.InvariantCulture)}"
                : string.Empty;

            var file = await _exportService.GenerateResultExport(
                contestId,
                exportTemplate,
                exportTemplateKeyCantonSuffix,
                _auth.Tenant.Id,
                viewablePartialResultsCountingCircleIds,
                asyncPdfGenerationInfo,
                ct);

            ProtocolExportMeter.AddExportStarted();

            aggregate.Start(
                protocolExportId,
                contestId,
                file.Filename,
                callbackToken,
                exportTemplate.ExportTemplateId,
                requestId,
                exportTemplate.Template.Key + exportTemplateKeyCantonSuffix,
                exportTemplate.CountingCircleId,
                exportTemplate.PoliticalBusinessId,
                exportTemplate.PoliticalBusinessUnionId,
                exportTemplate.DomainOfInfluenceType ?? DomainOfInfluenceType.Unspecified,
                exportTemplate.PoliticalBusinessResultBundleId);

            await _aggregateRepository.Save(aggregate);
        }

        return protocolExportIds;
    }

    public async Task HandleCallback(
        string serializedCallbackData,
        Guid protocolExportId,
        string callbackToken)
    {
        var callbackData = DmDocJsonSerializer.Deserialize<CallbackData>(serializedCallbackData);

        _permissionService.SetAbraxasAuthIfNotAuthenticated();
        var aggregate = await _aggregateRepository.GetById<ProtocolExportAggregate>(protocolExportId);

        try
        {
            if (callbackData.Action == CallbackAction.FinishEditing && callbackData.Data?.PrintJobId is { } printJobId)
            {
                if (aggregate.IsCompleted)
                {
                    _logger.LogDebug("Protocol export is already completed for {ProtocolExportId}", protocolExportId);
                }
                else
                {
                    var duration = DateTime.Now - aggregate.CreatedDate;
                    aggregate.Complete(callbackToken, printJobId);
                    ProtocolExportMeter.AddExportDuration(duration, aggregate.ExportKey);
                    ProtocolExportMeter.AddExportCompleted();
                    _logger.LogDebug("Successfully completed protocol export {ProtocolExportId}", protocolExportId);
                    _draftCleanupQueue.Enqueue(callbackData.ObjectId, DraftCleanupMode.Content);
                }
            }
            else
            {
                aggregate.Fail(callbackToken);
                ProtocolExportMeter.AddExportFailed();
                _logger.LogWarning(
                    "Failed protocol export {ProtocolExportId} due to callback: {CallbackData}",
                    protocolExportId,
                    serializedCallbackData);
            }
        }
        catch (InvalidCallbackTokenException)
        {
            _logger.LogInformation("Callback token does not match for protocol export {Id}. Queuing it for cleanup.", protocolExportId);

            ProtocolExportMeter.AddExportInvalidCallbackToken();

            // This draft is outdated (there is a newer version), let's clean this up
            _draftCleanupQueue.Enqueue(callbackData.ObjectId, DraftCleanupMode.Hard);
        }

        await _aggregateRepository.Save(aggregate);
    }
}
