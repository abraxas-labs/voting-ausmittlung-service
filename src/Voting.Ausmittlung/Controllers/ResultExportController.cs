﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Utils;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Rest.Files;

namespace Voting.Ausmittlung.Controllers;

[ApiController]
[Route("api/result_export")]
public class ResultExportController : ControllerBase
{
    private readonly ResultExportService _resultExportService;
    private readonly ProtocolExportService _protocolExportService;
    private readonly IMapper _mapper;

    public ResultExportController(
        ResultExportService resultExportService,
        ProtocolExportService protocolExportService,
        IMapper mapper)
    {
        _resultExportService = resultExportService;
        _protocolExportService = protocolExportService;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.Export.ExportData)]
    [HttpPost]
    public async Task<FileResult> DownloadExports(GenerateResultExportsRequest request, CancellationToken ct)
    {
        var isMultiExport = request.ExportTemplateIds.Count != 1;
        var fileModels = _resultExportService.GenerateExports(
                request.ContestId,
                request.CountingCircleId,
                request.ExportTemplateIds,
                false,
                ct)
            .Select(f => new FileModelWrapper(f), ct);

        return await FileResultUtil.CreateFileResult(fileModels, isMultiExport, ct);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Review)]
    [HttpPost("bundle_review")]
    public async Task<FileResult> DownloadResultBundleReviewExport(GenerateResultBundleReviewExportRequest request, CancellationToken ct)
    {
        var fileModel = await _resultExportService.GenerateResultBundleReviewExport(request.ContestId, _mapper.Map<BundleReviewExportRequest>(request), ct);
        return SingleFileResult.Create(new FileModelWrapper(fileModel), ct);
    }

    [AuthorizePermission(Permissions.Export.ExportData)]
    [HttpPost("protocol_exports")]
    public async Task<FileResult> DownloadProtocolExport(FetchProtocolExportsRequest request, CancellationToken ct)
    {
        var isMultiExport = request.ProtocolExportIds.Count != 1;
        var fileModels = _protocolExportService.GetProtocolExports(
                request.ContestId,
                request.CountingCircleId,
                request.ProtocolExportIds,
                ct)
            .Select(f => new FileModelWrapper(f), ct);

        return await FileResultUtil.CreateFileResult(fileModels, isMultiExport, ct);
    }

    // Note: DmDoc currently does not support authorization in webhooks.
    // We use a unique token per callback to make sure that the requests come from DmDoc.
    [AllowAnonymous]
    [HttpPost("webhook_callback")]
    public async Task WebhookCallback(Guid protocolExportId, string callbackToken)
    {
        // DmDoc uses snake_case naming in JSON, handle that separately since we use camelCase
        using var streamReader = new StreamReader(Request.Body);
        var callbackData = await streamReader.ReadToEndAsync();
        await _protocolExportService.HandleCallback(callbackData, protocolExportId, callbackToken);
    }
}
