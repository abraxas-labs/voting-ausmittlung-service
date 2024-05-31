﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Controllers.Models.Export;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Export.Models;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Utils;
using Voting.Lib.Iam.Authorization;

namespace Voting.Ausmittlung.Controllers;

[ApiController]
[Route("api/export")]
public class ExportController
{
    private readonly ResultExportTemplateReader _templateReader;
    private readonly ResultExportService _resultExportService;
    private readonly ProtocolExportService _protocolExportService;
    private readonly Ech0252ExportService _ech0252ExportService;
    private readonly IMapper _mapper;

    public ExportController(
        ResultExportTemplateReader templateReader,
        IMapper mapper,
        ResultExportService resultExportService,
        ProtocolExportService protocolExportService,
        Ech0252ExportService ech0252ExportService)
    {
        _templateReader = templateReader;
        _mapper = mapper;
        _resultExportService = resultExportService;
        _protocolExportService = protocolExportService;
        _ech0252ExportService = ech0252ExportService;
    }

    [AuthorizePermission(Permissions.ReportExportApi.ExportData)]
    [HttpGet("data/list")]
    public async Task<ListDataExportsResponse> ListDataExports([FromQuery] ListDataExportsRequest request)
    {
        var container = await _templateReader.ListDataExportTemplates(
            request.ContestId,
            request.CountingCircleId);
        return _mapper.Map<ListDataExportsResponse>(container);
    }

    [AuthorizePermission(Permissions.ReportExportApi.ExportProtocol)]
    [HttpGet("protocol/list")]
    public async Task<ListProtocolExportsResponse> ListProtocolExports([FromQuery] ListProtocolExportsRequest request)
    {
        var container = await _templateReader.ListProtocolExports(
            request.ContestId,
            request.CountingCircleId);
        return _mapper.Map<ListProtocolExportsResponse>(container);
    }

    [AuthorizePermission(Permissions.ReportExportApi.ExportData)]
    [HttpPost("data/download")]
    public async Task<FileResult> DownloadDataExport(DownloadDataExportRequest request, CancellationToken ct)
    {
        var fileModels = _resultExportService.GenerateExports(
                request.ContestId,
                request.CountingCircleId,
                [request.ExportTemplateId],
                true,
                ct)
                .Select(f => new FileModelWrapper(f), ct);

        return await FileResultUtil.CreateFileResult(fileModels, false, ct);
    }

    [AuthorizePermission(Permissions.ReportExportApi.ExportProtocol)]
    [HttpPost("protocol/generate")]
    public async Task<GenerateProtocolExportResponse> GenerateProtocolExport(GenerateProtocolExportRequest request)
    {
        var protocolExportIds = await _protocolExportService.StartExports(
            request.ContestId,
            request.CountingCircleId,
            [request.ExportTemplateId],
            true);

        return new GenerateProtocolExportResponse { ProtocolExportId = protocolExportIds.Single(), };
    }

    [AuthorizePermission(Permissions.ReportExportApi.ExportProtocol)]
    [HttpGet("protocol/status")]
    public async Task<ListProtocolExportStatesResponse> ListProtocolExportStates([FromQuery] ListProtocolExportStatesRequest request)
    {
        var container = await _templateReader.ListProtocolExports(
            request.ContestId,
            request.CountingCircleId);
        return _mapper.Map<ListProtocolExportStatesResponse>(container);
    }

    [AuthorizePermission(Permissions.ReportExportApi.ExportProtocol)]
    [HttpPost("protocol/download")]
    public async Task<FileResult> DownloadProtocolExport(DownloadProtocolExportRequest request, CancellationToken ct)
    {
        var fileModels = _protocolExportService.GetProtocolExports(
                request.ContestId,
                request.CountingCircleId,
                [request.ProtocolExportId],
                ct)
            .Select(f => new FileModelWrapper(f), ct);

        return await FileResultUtil.CreateFileResult(fileModels, false, ct);
    }

    [AuthorizePermission(Permissions.ReportExportApi.ExportEch0252)]
    [HttpPost("ech0252/download")]
    public async Task<FileResult> DownloadEch0252Export(DownloadEch0252ExportRequest request, CancellationToken ct)
    {
        var filter = _ech0252ExportService.BuildAndValidateFilter(_mapper.Map<Ech0252FilterRequest>(request));
        var fileModels = _ech0252ExportService.GenerateExports(filter, ct)
            .Select(f => new FileModelWrapper(f), ct);

        return await FileResultUtil.CreateFileResult(fileModels, true, ct);
    }
}
