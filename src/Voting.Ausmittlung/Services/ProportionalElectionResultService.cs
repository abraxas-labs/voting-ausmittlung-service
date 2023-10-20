// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Ausmittlung.Resources;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ProportionalElectionResultService.ProportionalElectionResultServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class ProportionalElectionResultService : ServiceBase
{
    private readonly ProportionalElectionResultReader _proportionalElectionResultReader;
    private readonly ProportionalElectionResultWriter _proportionalElectionResultWriter;
    private readonly ProportionalElectionEndResultReader _proportionalElectionEndResultReader;
    private readonly ProportionalElectionEndResultWriter _proportionalElectionEndResultWriter;
    private readonly ProportionalElectionResultValidationSummaryBuilder _proportionalElectionResultValidationSummaryBuilder;
    private readonly IMapper _mapper;

    public ProportionalElectionResultService(
        ProportionalElectionResultReader proportionalElectionResultReader,
        ProportionalElectionResultWriter proportionalElectionResultWriter,
        ProportionalElectionEndResultReader proportionalElectionEndResultReader,
        ProportionalElectionEndResultWriter proportionalElectionEndResultWriter,
        ProportionalElectionResultValidationSummaryBuilder proportionalElectionResultValidationSummaryBuilder,
        IMapper mapper)
    {
        _proportionalElectionResultReader = proportionalElectionResultReader;
        _proportionalElectionResultWriter = proportionalElectionResultWriter;
        _proportionalElectionEndResultReader = proportionalElectionEndResultReader;
        _proportionalElectionEndResultWriter = proportionalElectionEndResultWriter;
        _proportionalElectionResultValidationSummaryBuilder = proportionalElectionResultValidationSummaryBuilder;
        _mapper = mapper;
    }

    public override async Task<ProtoModels.ProportionalElectionResult> Get(GetProportionalElectionResultRequest request, ServerCallContext context)
    {
        var result = string.IsNullOrEmpty(request.ElectionResultId)
            ? await _proportionalElectionResultReader.Get(GuidParser.Parse(request.ElectionId), GuidParser.Parse(request.CountingCircleId))
            : await _proportionalElectionResultReader.Get(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.ProportionalElectionResult>(result);
    }

    public override async Task<ProtoModels.ProportionalElectionUnmodifiedListResults> GetUnmodifiedLists(GetProportionalElectionUnmodifiedListResultsRequest request, ServerCallContext context)
    {
        var result = await _proportionalElectionResultReader.GetWithUnmodifiedLists(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.ProportionalElectionUnmodifiedListResults>(result);
    }

    public override async Task<ProtoModels.ProportionalElectionListResults> GetListResults(GetProportionalElectionListResultsRequest request, ServerCallContext context)
    {
        var listResults = await _proportionalElectionResultReader.GetListResults(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.ProportionalElectionListResults>(listResults);
    }

    public override async Task<Empty> DefineEntry(DefineProportionalElectionResultEntryRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.DefineEntry(
            GuidParser.Parse(request.ElectionResultId),
            _mapper.Map<ProportionalElectionResultEntryParams>(request.ResultEntryParams));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> EnterCountOfVoters(EnterProportionalElectionCountOfVotersRequest request, ServerCallContext context)
    {
        var id = GuidParser.Parse(request.ElectionResultId);
        var countOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(request.CountOfVoters);
        await _proportionalElectionResultWriter.EnterCountOfVoters(id, countOfVoters);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> EnterUnmodifiedListResults(
        EnterProportionalElectionUnmodifiedListResultsRequest request,
        ServerCallContext context)
    {
        var id = GuidParser.Parse(request.ElectionResultId);
        var results = _mapper.Map<IReadOnlyCollection<ProportionalElectionUnmodifiedListResult>>(request.Results);
        await _proportionalElectionResultWriter.EnterUnmodifiedListResults(id, results);
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.SecondFactorTransaction> PrepareSubmissionFinished(ProportionalElectionResultPrepareSubmissionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _proportionalElectionResultWriter.PrepareSubmissionFinished(GuidParser.Parse(request.ElectionResultId), Strings.ProportionalElectionResult_SubmissionFinished);
        return secondFactorTransaction == null ? new ProtoModels.SecondFactorTransaction() : new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    public override async Task<Empty> SubmissionFinished(ProportionalElectionResultSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.SubmissionFinished(GuidParser.Parse(request.ElectionResultId), request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> ResetToSubmissionFinished(ProportionalElectionResultResetToSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.ResetToSubmissionFinished(GuidParser.Parse(request.ElectionResultId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.SecondFactorTransaction> PrepareCorrectionFinished(ProportionalElectionResultPrepareCorrectionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _proportionalElectionResultWriter.PrepareCorrectionFinished(GuidParser.Parse(request.ElectionResultId), Strings.ProportionalElectionResult_CorrectionFinished);
        return secondFactorTransaction == null ? new ProtoModels.SecondFactorTransaction() : new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    public override async Task<Empty> CorrectionFinished(ProportionalElectionResultCorrectionFinishedRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.CorrectionFinished(GuidParser.Parse(request.ElectionResultId), request.Comment, request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> FlagForCorrection(ProportionalElectionResultFlagForCorrectionRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.FlagForCorrection(GuidParser.Parse(request.ElectionResultId), request.Comment);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> AuditedTentatively(ProportionalElectionResultAuditedTentativelyRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.AuditedTentatively(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> Plausibilise(ProportionalElectionResultsPlausibiliseRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.Plausibilise(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> ResetToAuditedTentatively(ProportionalElectionResultsResetToAuditedTentativelyRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.ResetToAuditedTentatively(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.ProportionalElectionEndResult> GetEndResult(GetProportionalElectionEndResultRequest request, ServerCallContext context)
    {
        var endResult = await _proportionalElectionEndResultReader.GetEndResult(GuidParser.Parse(request.ProportionalElectionId));
        return _mapper.Map<ProtoModels.ProportionalElectionEndResult>(endResult);
    }

    public override async Task<ProtoModels.ProportionalElectionListEndResultAvailableLotDecisions> GetListEndResultAvailableLotDecisions(
        GetProportionalElectionListEndResultAvailableLotDecisionsRequest request,
        ServerCallContext context)
    {
        var availableLotDecisions = await _proportionalElectionEndResultReader.GetEndResultAvailableLotDecisions(GuidParser.Parse(request.ProportionalElectionListId));
        return _mapper.Map<ProtoModels.ProportionalElectionListEndResultAvailableLotDecisions>(availableLotDecisions);
    }

    public override async Task<Empty> UpdateListEndResultLotDecisions(
        UpdateProportionalElectionListEndResultLotDecisionsRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.UpdateEndResultLotDecisions(
            GuidParser.Parse(request.ProportionalElectionListId),
            _mapper.Map<List<ElectionEndResultLotDecision>>(request.LotDecisions));
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.SecondFactorTransaction> PrepareFinalizeEndResult(PrepareFinalizeProportionalElectionEndResultRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _proportionalElectionEndResultWriter.PrepareFinalize(GuidParser.Parse(request.ProportionalElectionId), Strings.ProportionalElectionResult_FinalizeEndResult);
        return new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    public override async Task<Empty> FinalizeEndResult(FinalizeProportionalElectionEndResultRequest request, ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.Finalize(GuidParser.Parse(request.ProportionalElectionId), request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> RevertEndResultFinalization(RevertProportionalElectionEndResultFinalizationRequest request, ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.RevertFinalization(GuidParser.Parse(request.ProportionalElectionId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.ValidationSummary> ValidateEnterCountOfVoters(ValidateEnterProportionalElectionCountOfVotersRequest request, ServerCallContext context)
    {
        var id = GuidParser.Parse(request.Request.ElectionResultId);
        var countOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(request.Request.CountOfVoters);
        var summary = await _proportionalElectionResultValidationSummaryBuilder.BuildEnterCountOfVotersValidationSummary(id, countOfVoters);
        return _mapper.Map<ProtoModels.ValidationSummary>(summary);
    }

    public override async Task<Empty> EnterManualListEndResult(EnterProportionalElectionManualListEndResultRequest request, ServerCallContext context)
    {
        var listId = GuidParser.Parse(request.ProportionalElectionListId);
        var candidateEndResults = _mapper.Map<List<ProportionalElectionManualCandidateEndResult>>(request.CandidateEndResults);
        await _proportionalElectionEndResultWriter.EnterManualListEndResult(listId, candidateEndResults);
        return ProtobufEmpty.Instance;
    }
}
