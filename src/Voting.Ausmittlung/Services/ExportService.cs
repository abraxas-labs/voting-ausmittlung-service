// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Ausmittlung.Data.Models;
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

    public ExportService(
        IMapper mapper,
        ResultExportTemplateReader templateReader,
        ResultExportConfigurationReader configReader,
        ResultExportConfigurationWriter configWriter,
        ResultExportService exportService)
    {
        _mapper = mapper;
        _templateReader = templateReader;
        _exportService = exportService;
        _configWriter = configWriter;
        _configReader = configReader;
    }

    public override async Task<ProtoModels.ResultExportTemplates> GetCountingCircleResultExportTemplates(
        GetCountingCircleResultExportTemplatesRequest request, ServerCallContext context)
    {
        var result = await _templateReader.GetForCountingCircleResult(
            GuidParser.Parse(request.CountingCircleId),
            GuidParser.Parse(request.PoliticalBusinessId),
            _mapper.Map<PoliticalBusinessType>(request.PoliticalBusinessType));
        return _mapper.Map<ProtoModels.ResultExportTemplates>(result);
    }

    public override async Task<ProtoModels.ResultExportTemplates> GetPoliticalBusinessResultExportTemplates(
        GetPoliticalBusinessResultExportTemplatesRequest request, ServerCallContext context)
    {
        var result = await _templateReader.GetForPoliticalBusinessResult(
            GuidParser.Parse(request.PoliticalBusinessId),
            _mapper.Map<PoliticalBusinessType>(request.PoliticalBusinessType));
        return _mapper.Map<ProtoModels.ResultExportTemplates>(result);
    }

    public override async Task<ProtoModels.ResultExportTemplates> GetMultiplePoliticalBusinessesResultExportTemplates(
        GetMultiplePoliticalBusinessesResultExportTemplatesRequest request, ServerCallContext context)
    {
        var result = await _templateReader.GetForMultiplePoliticalBusinessesResult(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.ResultExportTemplates>(result);
    }

    public override async Task<ProtoModels.ResultExportTemplates> GetMultiplePoliticalBusinessesCountingCircleResultExportTemplates(
        GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest request, ServerCallContext context)
    {
        var result = await _templateReader.GetForMultiplePoliticalBusinessesCountingCircleResult(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId));
        return _mapper.Map<ProtoModels.ResultExportTemplates>(result);
    }

    public override async Task<ProtoModels.ResultExportTemplates> GetContestExportTemplates(GetContestExportTemplatesRequest request, ServerCallContext context)
    {
        var result = await _templateReader.GetForContest(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.ResultExportTemplates>(result);
    }

    public override async Task<ProtoModels.ResultExportTemplates> GetPoliticalBusinessUnionResultExportTemplates(
        GetPoliticalBusinessUnionResultExportTemplatesRequest request, ServerCallContext context)
    {
        var result = await _templateReader.GetForPoliticalBusinessUnionResult(
            GuidParser.Parse(request.PoliticalBusinessUnionId),
            _mapper.Map<PoliticalBusinessType>(request.PoliticalBusinessType));
        return _mapper.Map<ProtoModels.ResultExportTemplates>(result);
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
        var resultExportConfig = _mapper.Map<Core.Domain.ResultExportConfiguration>(request);
        await _configWriter.Update(resultExportConfig);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> TriggerResultExport(TriggerResultExportRequest request, ServerCallContext context)
    {
        await _exportService.TriggerExportsFromConfiguration(
            GuidParser.Parse(request.ExportConfigurationId),
            GuidParser.Parse(request.ContestId),
            request.PoliticalBusinessIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }
}
