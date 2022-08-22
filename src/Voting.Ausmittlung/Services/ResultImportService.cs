// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write.Import;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ResultImportService.ResultImportServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class ResultImportService : ServiceBase
{
    private readonly IMapper _mapper;
    private readonly ResultImportReader _resultImportReader;
    private readonly ResultImportWriter _resultImportWriter;

    public ResultImportService(IMapper mapper, ResultImportReader resultImportReader, ResultImportWriter resultImportWriter)
    {
        _mapper = mapper;
        _resultImportReader = resultImportReader;
        _resultImportWriter = resultImportWriter;
    }

    public override async Task<ProtoModels.ResultImports> ListImports(ListResultImportsRequest request, ServerCallContext context)
    {
        var imports = await _resultImportReader.GetResultImports(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.ResultImports>(imports);
    }

    public override async Task<Empty> DeleteImportData(DeleteResultImportDataRequest request, ServerCallContext context)
    {
        await _resultImportWriter.DeleteResults(GuidParser.Parse(request.ContestId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.MajorityElectionContestWriteInMappings> GetMajorityElectionWriteInMappings(
        GetMajorityElectionWriteInMappingsRequest request,
        ServerCallContext context)
    {
        var writeIns = await _resultImportReader.GetMajorityElectionWriteInMappings(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId));
        return _mapper.Map<ProtoModels.MajorityElectionContestWriteInMappings>(writeIns);
    }

    public override async Task<Empty> MapMajorityElectionWriteIns(MapMajorityElectionWriteInsRequest request, ServerCallContext context)
    {
        var mappings = _mapper.Map<IReadOnlyCollection<MajorityElectionWriteIn>>(request.Mappings);
        await _resultImportWriter.MapMajorityElectionWriteIns(
            GuidParser.Parse(request.ImportId),
            GuidParser.Parse(request.ElectionId),
            GuidParser.Parse(request.CountingCircleId),
            _mapper.Map<Data.Models.PoliticalBusinessType>(request.PoliticalBusinessType),
            mappings);
        return ProtobufEmpty.Instance;
    }
}
