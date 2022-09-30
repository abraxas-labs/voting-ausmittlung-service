// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Rest.Files;

namespace Voting.Ausmittlung.Controllers;

[ApiController]
[Route("api/result_export")]
[Authorize]
public class ResultExportController : ControllerBase
{
    private readonly ResultExportService _resultExportService;
    private readonly IMapper _mapper;

    public ResultExportController(ResultExportService resultExportService, IMapper mapper)
    {
        _resultExportService = resultExportService;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<FileResult> DownloadExports(GenerateResultExportsRequest request, CancellationToken ct)
    {
        var isMultiExport = request.ResultExportRequests.Count != 1;
        var fileModels = _resultExportService.GenerateExports(request.ContestId, _mapper.Map<IReadOnlyCollection<ResultExportRequest>>(request.ResultExportRequests), ct)
            .Select(f => new FileModelWrapper(f), ct);
        if (isMultiExport)
        {
            return SingleFileResult.CreateZipFile(fileModels, "export.zip", ct);
        }

        var enumerator = fileModels.GetAsyncEnumerator(ct);
        if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("At least one file is required");
        }

        var file = enumerator.Current;
        if (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("At maximum one files is supported if " + nameof(isMultiExport) + " is false");
        }

        return SingleFileResult.Create(file, ct);
    }

    [HttpPost("bundle_review")]
    public async Task<FileResult> DownloadResultBundleReviewExport(GenerateResultBundleReviewExportRequest request, CancellationToken ct)
    {
        var fileModel = await _resultExportService.GenerateResultBundleReviewExport(request.ContestId, _mapper.Map<ResultExportRequest>(request), ct);
        return SingleFileResult.Create(new FileModelWrapper(fileModel), ct);
    }
}
