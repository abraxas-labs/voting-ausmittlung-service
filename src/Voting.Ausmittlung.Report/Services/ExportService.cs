// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public async IAsyncEnumerable<FileModel> GenerateResultExportsIgnoreErrors(
        IEnumerable<string> keys,
        ResultExportConfiguration export,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var politicalBusinesses = export.PoliticalBusinesses!.Select(x => x.PoliticalBusiness!).ToList();
        foreach (var key in keys)
        {
            await foreach (var file in _resultRenderServiceAdapter.RenderAllIgnoreErrors(export.Contest!, key, politicalBusinesses, ct))
            {
                yield return file;
            }
        }
    }
}
