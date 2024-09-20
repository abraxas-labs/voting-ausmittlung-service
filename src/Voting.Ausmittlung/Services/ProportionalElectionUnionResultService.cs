// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Ausmittlung.Resources;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceBase;

namespace Voting.Ausmittlung.Services;

public class ProportionalElectionUnionResultService : ServiceBase
{
    private readonly ProportionalElectionUnionEndResultReader _endResultReader;
    private readonly ProportionalElectionUnionEndResultWriter _endResultWriter;
    private readonly IMapper _mapper;
    private readonly DoubleProportionalResultReader _dpResultReader;
    private readonly DoubleProportionalResultWriter _dpResultWriter;

    public ProportionalElectionUnionResultService(
        ProportionalElectionUnionEndResultReader endResultReader,
        IMapper mapper,
        ProportionalElectionUnionEndResultWriter endResultWriter,
        DoubleProportionalResultReader dpResultReader,
        DoubleProportionalResultWriter dpResultWriter)
    {
        _endResultReader = endResultReader;
        _mapper = mapper;
        _endResultWriter = endResultWriter;
        _dpResultReader = dpResultReader;
        _dpResultWriter = dpResultWriter;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResult.Read)]
    public override async Task<ProtoModels.ProportionalElectionUnionEndResult> GetPartialEndResult(GetProportionalElectionUnionPartialEndResultRequest request, ServerCallContext context)
    {
        var partialResult = await _endResultReader.GetPartialEndResult(GuidParser.Parse(request.ProportionalElectionUnionId));

        var mapped = _mapper.Map<ProtoModels.ProportionalElectionUnionEndResult>(partialResult);
        mapped.PartialResult = true;
        return mapped;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResult.Read)]
    public override async Task<ProtoModels.ProportionalElectionUnionEndResult> GetEndResult(GetProportionalElectionUnionEndResultRequest request, ServerCallContext context)
    {
        var endResult = await _endResultReader.GetEndResult(GuidParser.Parse(request.ProportionalElectionUnionId));
        return _mapper.Map<ProtoModels.ProportionalElectionUnionEndResult>(endResult);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResult.Read)]
    public override async Task<ProtoModels.DoubleProportionalResult> GetDoubleProportionalResult(GetProportionalElectionUnionDoubleProportionalResultRequest request, ServerCallContext context)
    {
        var dpResult = await _dpResultReader.GetUnionDoubleProportionalResult(GuidParser.Parse(request.ProportionalElectionUnionId));
        return _mapper.Map<ProtoModels.DoubleProportionalResult>(dpResult);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResult.Finalize)]
    public override async Task<ProtoModels.SecondFactorTransaction> PrepareFinalizeEndResult(PrepareFinalizeProportionalElectionUnionEndResultRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code, qrCode) = await _endResultWriter.PrepareFinalize(GuidParser.Parse(request.ProportionalElectionUnionId), Strings.ProportionalElectionUnionResult_FinalizeEndResult);
        return new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code, QrCode = qrCode };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResult.Finalize)]
    public override async Task<Empty> FinalizeEndResult(FinalizeProportionalElectionUnionEndResultRequest request, ServerCallContext context)
    {
        await _endResultWriter.Finalize(GuidParser.Parse(request.ProportionalElectionUnionId), request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResult.Finalize)]
    public override async Task<Empty> RevertEndResultFinalization(RevertProportionalElectionUnionEndResultFinalizationRequest request, ServerCallContext context)
    {
        await _endResultWriter.RevertFinalization(GuidParser.Parse(request.ProportionalElectionUnionId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResultLotDecision.Read)]
    public override async Task<ProtoModels.DoubleProportionalResultSuperApportionmentAvailableLotDecisions> GetDoubleProportionalResultSuperApportionmentAvailableLotDecisions(GetProportionalElectionUnionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest request, ServerCallContext context)
    {
        var availableLotDecisions = await _dpResultReader.GetUnionDoubleProportionalSuperApportionmentAvailableLotDecisions(GuidParser.Parse(request.ProportionalElectionUnionId));
        return _mapper.Map<ProtoModels.DoubleProportionalResultSuperApportionmentAvailableLotDecisions>(availableLotDecisions);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResultLotDecision.Read)]
    public override async Task<ProtoModels.DoubleProportionalResultSubApportionmentAvailableLotDecisions> GetDoubleProportionalResultSubApportionmentAvailableLotDecisions(
        GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest request,
        ServerCallContext context)
    {
        var availableLotDecisions = await _dpResultReader.GetUnionDoubleProportionalSubApportionmentAvailableLotDecisions(GuidParser.Parse(request.ProportionalElectionUnionId));
        return _mapper.Map<ProtoModels.DoubleProportionalResultSubApportionmentAvailableLotDecisions>(availableLotDecisions);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResultLotDecision.Update)]
    public override async Task<Empty> UpdateDoubleProportionalResultSuperApportionmentLotDecision(UpdateProportionalElectionUnionDoubleProportionalResultSuperApportionmentLotDecisionRequest request, ServerCallContext context)
    {
        await _dpResultWriter.UpdateUnionSuperApportionmentLotDecision(GuidParser.Parse(request.ProportionalElectionUnionId), request.Number);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessUnionEndResultLotDecision.Update)]
    public override async Task<Empty> UpdateDoubleProportionalResultSubApportionmentLotDecision(UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest request, ServerCallContext context)
    {
        await _dpResultWriter.UpdateUnionSubApportionmentLotDecision(GuidParser.Parse(request.ProportionalElectionUnionId), request.Number);
        return ProtobufEmpty.Instance;
    }
}
