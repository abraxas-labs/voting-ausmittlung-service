// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ExportService.ExportServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class ExportService : ServiceBase
{
    private readonly IMapper _mapper;
    private readonly ResultExportTemplateReader _templateReader;
    private readonly ResultExportConfigurationWriter _configWriter;
    private readonly ResultExportConfigurationReader _configReader;
    private readonly ResultExportService _exportService;
    private readonly ProtocolExportService _protocolExportService;

    public ExportService(
        IMapper mapper,
        ResultExportTemplateReader templateReader,
        ResultExportConfigurationReader configReader,
        ResultExportConfigurationWriter configWriter,
        ResultExportService exportService,
        ProtocolExportService protocolExportService)
    {
        _mapper = mapper;
        _templateReader = templateReader;
        _exportService = exportService;
        _protocolExportService = protocolExportService;
        _configWriter = configWriter;
        _configReader = configReader;
    }

    public override async Task<ProtoModels.DataExportTemplates> ListDataExportTemplates(
        ListDataExportTemplatesRequest request,
        ServerCallContext context)
    {
        var container = await _templateReader.ListDataExportTemplates(
            GuidParser.Parse(request.ContestId),
            GuidParser.ParseNullable(request.CountingCircleId));
        return _mapper.Map<ProtoModels.DataExportTemplates>(container);
    }

    public override async Task<ProtoModels.ProtocolExports> ListProtocolExports(
        ListProtocolExportsRequest request,
        ServerCallContext context)
    {
        var container = await _templateReader.ListProtocolExports(
            GuidParser.Parse(request.ContestId),
            GuidParser.ParseNullable(request.CountingCircleId));
        return _mapper.Map<ProtoModels.ProtocolExports>(container);
    }

    public override Task GetProtocolExportStateChanges(
        GetProtocolExportStateChangesRequest request,
        IServerStreamWriter<ProtoModels.ProtocolExportStateChange> responseStream,
        ServerCallContext context)
    {
        return _templateReader.ListenToProtocolExportStateChanges(
            GuidParser.Parse(request.ContestId),
            GuidParser.ParseNullable(request.CountingCircleId),
            e => responseStream.WriteAsync(_mapper.Map<ProtoModels.ProtocolExportStateChange>(e)),
            context.CancellationToken);
    }

    public override async Task<Empty> StartProtocolExports(
        StartProtocolExportsRequest request,
        ServerCallContext context)
    {
        await _protocolExportService.StartExports(
            GuidParser.Parse(request.ContestId),
            GuidParser.ParseNullable(request.CountingCircleId),
            _mapper.Map<List<Guid>>(request.ExportTemplateIds),
            context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.ResultExportConfigurations> ListResultExportConfigurations(
        ListResultExportConfigurationsRequest request,
        ServerCallContext context)
    {
        var configs = await _configReader.List(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.ResultExportConfigurations>(configs);
    }

    public override async Task<Empty> UpdateResultExportConfiguration(UpdateResultExportConfigurationRequest request, ServerCallContext context)
    {
        var resultExportConfig = _mapper.Map<ResultExportConfiguration>(request);
        await _configWriter.Update(resultExportConfig);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> TriggerResultExport(TriggerResultExportRequest request, ServerCallContext context)
    {
        var metadata = _mapper.Map<Dictionary<Guid, ResultExportConfigurationPoliticalBusinessMetadata>>(request.PoliticalBusinessMetadata);
        await _exportService.TriggerExportsFromConfiguration(
            GuidParser.Parse(request.ExportConfigurationId),
            GuidParser.Parse(request.ContestId),
            request.PoliticalBusinessIds.Select(GuidParser.Parse).ToList(),
            metadata);
        return ProtobufEmpty.Instance;
    }
}
