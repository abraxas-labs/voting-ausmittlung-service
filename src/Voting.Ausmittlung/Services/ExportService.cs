// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Services.V1.Responses;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ExportService.ExportServiceBase;

namespace Voting.Ausmittlung.Services;

public class ExportService : ServiceBase
{
    private readonly IMapper _mapper;
    private readonly ResultExportTemplateReader _templateReader;
    private readonly ResultExportConfigurationWriter _configWriter;
    private readonly ResultExportConfigurationReader _configReader;
    private readonly ResultExportService _exportService;
    private readonly ProtocolExportService _protocolExportService;
    private readonly ResultExportService _resultExportService;

    public ExportService(
        IMapper mapper,
        ResultExportTemplateReader templateReader,
        ResultExportConfigurationReader configReader,
        ResultExportConfigurationWriter configWriter,
        ResultExportService exportService,
        ProtocolExportService protocolExportService,
        ResultExportService resultExportService)
    {
        _mapper = mapper;
        _templateReader = templateReader;
        _exportService = exportService;
        _protocolExportService = protocolExportService;
        _resultExportService = resultExportService;
        _configWriter = configWriter;
        _configReader = configReader;
    }

    [AuthorizePermission(Permissions.Export.ExportData)]
    public override async Task<ProtoModels.DataExportTemplates> ListDataExportTemplates(
        ListDataExportTemplatesRequest request,
        ServerCallContext context)
    {
        var container = await _templateReader.ListDataExportTemplates(
            GuidParser.Parse(request.ContestId),
            GuidParser.ParseNullable(request.CountingCircleId));
        return _mapper.Map<ProtoModels.DataExportTemplates>(container);
    }

    [AuthorizePermission(Permissions.Export.ExportData)]
    public override async Task<ProtoModels.ProtocolExports> ListProtocolExports(
        ListProtocolExportsRequest request,
        ServerCallContext context)
    {
        var container = await _templateReader.ListProtocolExports(
            GuidParser.Parse(request.ContestId),
            GuidParser.ParseNullable(request.CountingCircleId));
        return _mapper.Map<ProtoModels.ProtocolExports>(container);
    }

    [AuthorizePermission(Permissions.Export.ExportData)]
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

    [AuthorizePermission(Permissions.Export.ExportData)]
    public override async Task<Empty> StartProtocolExports(
        StartProtocolExportsRequest request,
        ServerCallContext context)
    {
        await _protocolExportService.StartExports(
            GuidParser.Parse(request.ContestId),
            GuidParser.ParseNullable(request.CountingCircleId),
            _mapper.Map<List<Guid>>(request.ExportTemplateIds),
            false,
            context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ExportConfiguration.Read)]
    public override async Task<ProtoModels.ResultExportConfigurations> ListResultExportConfigurations(
        ListResultExportConfigurationsRequest request,
        ServerCallContext context)
    {
        var configs = await _configReader.List(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.ResultExportConfigurations>(configs);
    }

    [AuthorizePermission(Permissions.ExportConfiguration.Update)]
    public override async Task<Empty> UpdateResultExportConfiguration(UpdateResultExportConfigurationRequest request, ServerCallContext context)
    {
        var resultExportConfig = _mapper.Map<ResultExportConfiguration>(request);
        await _configWriter.Update(resultExportConfig);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ExportConfiguration.Trigger)]
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

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Review)]
    public override async Task<StartBundleReviewExportResponse> StartBundleReviewExport(
        StartBundleReviewExportRequest request,
        ServerCallContext context)
    {
        var protocolExportId = await _resultExportService.StartBundleReviewExport(
            GuidParser.Parse(request.PoliticalBusinessResultBundleId),
            _mapper.Map<Data.Models.PoliticalBusinessType>(request.PoliticalBusinessType),
            context.CancellationToken);
        return new StartBundleReviewExportResponse
        {
            ProtocolExportId = protocolExportId.ToString(),
        };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Review)]
    public override Task GetBundleReviewExportStateChanges(
        GetBundleReviewExportStateChangesRequest request,
        IServerStreamWriter<ProtoModels.ProtocolExportStateChange> responseStream,
        ServerCallContext context)
    {
        return _resultExportService.ListenToBundleReviewExportStateChanges(
            GuidParser.Parse(request.PoliticalBusinessResultId),
            _mapper.Map<Data.Models.PoliticalBusinessType>(request.PoliticalBusinessType),
            e => responseStream.WriteAsync(_mapper.Map<ProtoModels.ProtocolExportStateChange>(e)),
            context.CancellationToken);
    }
}
