// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services;

public class ExportService
{
    private readonly ResultRenderServiceAdapter _resultRenderServiceAdapter;

    public ExportService(
        ResultRenderServiceAdapter resultRenderServiceAdapter)
    {
        _resultRenderServiceAdapter = resultRenderServiceAdapter;
    }

    public Task<FileModel> GenerateResultExport(
        Guid contestId,
        ResultExportTemplate exportTemplate,
        string? exportTemplateKeyCantonSuffix,
        string? tenantId = null,
        HashSet<Guid>? viewablePartialResultsCountingCircleIds = null,
        AsyncPdfGenerationInfo? asyncPdfGenerationInfo = null,
        CancellationToken ct = default)
    {
        var renderContext = new ReportRenderContext(contestId, exportTemplate.Template)
        {
            PoliticalBusinessIds = exportTemplate.PoliticalBusinessIds,
            BasisCountingCircleId = exportTemplate.CountingCircleId,
            DomainOfInfluenceType = exportTemplate.DomainOfInfluenceType ?? DomainOfInfluenceType.Unspecified,
            PoliticalBusinessUnionId = exportTemplate.PoliticalBusinessUnionId,
            PoliticalBusinessResultBundleId = exportTemplate.PoliticalBusinessResultBundleId,
            AsyncPdfGenerationInfo = asyncPdfGenerationInfo,
            ExportTemplateKeyCantonSuffix = exportTemplateKeyCantonSuffix,
            TenantId = tenantId,
            ViewablePartialResultsCountingCircleIds = viewablePartialResultsCountingCircleIds,
        };
        return _resultRenderServiceAdapter.Render(contestId, renderContext, ct);
    }

    public IReadOnlyList<ReportRenderContext> BuildRenderContexts(IEnumerable<string> keys, ResultExportConfiguration export)
    {
        var politicalBusinesses = export.PoliticalBusinesses!.Select(x => x.PoliticalBusiness!).ToList();
        return keys
            .SelectMany(key => _resultRenderServiceAdapter.BuildRenderContexts(export.Contest!, key, politicalBusinesses))
            .ToList();
    }

    public Task<FileModel> GenerateResultExport(ReportRenderContext context, CancellationToken ct = default)
    {
        if (context.RendererService == null)
        {
            throw new InvalidOperationException($"{nameof(context.RendererService)} must not be null when calling this method");
        }

        return context.RendererService.Render(context, ct);
    }
}
