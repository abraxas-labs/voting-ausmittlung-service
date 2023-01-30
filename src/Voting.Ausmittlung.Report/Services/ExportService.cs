// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;

namespace Voting.Ausmittlung.Report.Services;

public class ExportService
{
    private readonly ResultRenderServiceAdapter _resultRenderServiceAdapter;

    public ExportService(
        ResultRenderServiceAdapter resultRenderServiceAdapter)
    {
        _resultRenderServiceAdapter = resultRenderServiceAdapter;
    }

    public Task<FileModel> GenerateResultExport(Guid contestId, ResultExportRequest request, CancellationToken ct)
        => _resultRenderServiceAdapter.Render(contestId, request, ct);

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
