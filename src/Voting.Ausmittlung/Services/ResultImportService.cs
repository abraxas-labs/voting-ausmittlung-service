// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write.Import;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ResultImportType = Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ResultImportService.ResultImportServiceBase;

namespace Voting.Ausmittlung.Services;

public class ResultImportService : ServiceBase
{
    private readonly IMapper _mapper;
    private readonly ResultImportReader _resultImportReader;
    private readonly ResultImportWriter _resultImportWriter;
    private readonly ECountingResultImportWriter _eCountingResultImportWriter;
    private readonly EVotingResultImportWriter _eVotingResultImportWriter;

    public ResultImportService(IMapper mapper, ResultImportReader resultImportReader, ResultImportWriter resultImportWriter, ECountingResultImportWriter eCountingResultImportWriter, EVotingResultImportWriter eVotingResultImportWriter)
    {
        _mapper = mapper;
        _resultImportReader = resultImportReader;
        _resultImportWriter = resultImportWriter;
        _eCountingResultImportWriter = eCountingResultImportWriter;
        _eVotingResultImportWriter = eVotingResultImportWriter;
    }

    [AuthorizePermission(Permissions.Import.ReadEVoting)]
    public override async Task<ProtoModels.ResultImports> ListEVotingImports(ListEVotingResultImportsRequest request, ServerCallContext context)
    {
        var imports = await _resultImportReader.ListEVotingImports(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.ResultImports>(imports);
    }

    [AuthorizePermission(Permissions.Import.ReadECounting)]
    public override async Task<ProtoModels.ResultImports> ListECountingImports(ListECountingResultImportsRequest request, ServerCallContext context)
    {
        var imports = await _resultImportReader.ListECountingImports(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId));
        return _mapper.Map<ProtoModels.ResultImports>(imports);
    }

    [AuthorizePermission(Permissions.Import.DeleteEVoting)]
    public override async Task<Empty> DeleteEVotingImportData(DeleteEVotingResultImportDataRequest request, ServerCallContext context)
    {
        await _eVotingResultImportWriter.Delete(GuidParser.Parse(request.ContestId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.Import.DeleteECounting)]
    public override async Task<Empty> DeleteECountingImportData(DeleteECountingResultImportDataRequest request, ServerCallContext context)
    {
        await _eCountingResultImportWriter.Delete(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionWriteIn.Read)]
    public override async Task<ProtoModels.MajorityElectionContestWriteInMappings> GetMajorityElectionWriteInMappings(
        GetMajorityElectionWriteInMappingsRequest request,
        ServerCallContext context)
    {
        var importType = request.ImportType == ResultImportType.Unspecified ? null : (Data.Models.ResultImportType?)request.ImportType;
        var writeIns = await _resultImportReader.GetMajorityElectionWriteInMappings(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId),
            GuidParser.ParseNullable(request.ElectionId),
            importType);
        return _mapper.Map<ProtoModels.MajorityElectionContestWriteInMappings>(writeIns);
    }

    [AuthorizePermission(Permissions.MajorityElectionWriteIn.Update)]
    public override async Task<Empty> MapMajorityElectionWriteIns(MapMajorityElectionWriteInsRequest request, ServerCallContext context)
    {
        var mappings = _mapper.Map<IReadOnlyCollection<MajorityElectionWriteIn>>(request.Mappings);
        await _resultImportWriter.MapMajorityElectionWriteIns(
            GuidParser.Parse(request.ImportId),
            GuidParser.Parse(request.ElectionId),
            GuidParser.Parse(request.CountingCircleId),
            _mapper.Map<PoliticalBusinessType>(request.PoliticalBusinessType),
            mappings);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionWriteIn.Update)]
    public override async Task<Empty> ResetMajorityElectionWriteIns(
        ResetMajorityElectionWriteInMappingsRequest request,
        ServerCallContext context)
    {
        await _resultImportWriter.ResetMajorityElectionWriteIns(
            GuidParser.Parse(request.CountingCircleId),
            GuidParser.Parse(request.ElectionId),
            _mapper.Map<PoliticalBusinessType>(request.PoliticalBusinessType),
            GuidParser.Parse(request.ImportId));
        return ProtobufEmpty.Instance;
    }
}
