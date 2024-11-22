// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Resources;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using PoliticalBusinessCountOfVoters = Voting.Ausmittlung.Core.Domain.PoliticalBusinessCountOfVoters;
using ProportionalElectionResultEntryParams = Voting.Ausmittlung.Core.Domain.ProportionalElectionResultEntryParams;
using ProportionalElectionUnmodifiedListResult = Voting.Ausmittlung.Core.Domain.ProportionalElectionUnmodifiedListResult;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ProportionalElectionResultService.ProportionalElectionResultServiceBase;

namespace Voting.Ausmittlung.Services;

public class ProportionalElectionResultService : ServiceBase
{
    private readonly ProportionalElectionResultReader _proportionalElectionResultReader;
    private readonly ProportionalElectionResultWriter _proportionalElectionResultWriter;
    private readonly ProportionalElectionEndResultReader _proportionalElectionEndResultReader;
    private readonly ProportionalElectionEndResultWriter _proportionalElectionEndResultWriter;
    private readonly ProportionalElectionResultValidationSummaryBuilder _proportionalElectionResultValidationSummaryBuilder;
    private readonly IMapper _mapper;
    private readonly DoubleProportionalResultReader _dpResultReader;
    private readonly DoubleProportionalResultWriter _dpResultWriter;

    public ProportionalElectionResultService(
        ProportionalElectionResultReader proportionalElectionResultReader,
        ProportionalElectionResultWriter proportionalElectionResultWriter,
        ProportionalElectionEndResultReader proportionalElectionEndResultReader,
        ProportionalElectionEndResultWriter proportionalElectionEndResultWriter,
        ProportionalElectionResultValidationSummaryBuilder proportionalElectionResultValidationSummaryBuilder,
        IMapper mapper,
        DoubleProportionalResultReader dpResultReader,
        DoubleProportionalResultWriter dpResultWriter)
    {
        _proportionalElectionResultReader = proportionalElectionResultReader;
        _proportionalElectionResultWriter = proportionalElectionResultWriter;
        _proportionalElectionEndResultReader = proportionalElectionEndResultReader;
        _proportionalElectionEndResultWriter = proportionalElectionEndResultWriter;
        _proportionalElectionResultValidationSummaryBuilder = proportionalElectionResultValidationSummaryBuilder;
        _mapper = mapper;
        _dpResultReader = dpResultReader;
        _dpResultWriter = dpResultWriter;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Read)]
    public override async Task<ProtoModels.ProportionalElectionResult> Get(GetProportionalElectionResultRequest request, ServerCallContext context)
    {
        var result = string.IsNullOrEmpty(request.ElectionResultId)
            ? await _proportionalElectionResultReader.Get(GuidParser.Parse(request.ElectionId), GuidParser.Parse(request.CountingCircleId))
            : await _proportionalElectionResultReader.Get(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.ProportionalElectionResult>(result);
    }

    [AuthorizePermission(Permissions.ProportionalElectionListResult.Read)]
    public override async Task<ProtoModels.ProportionalElectionUnmodifiedListResults> GetUnmodifiedLists(GetProportionalElectionUnmodifiedListResultsRequest request, ServerCallContext context)
    {
        var result = await _proportionalElectionResultReader.GetWithUnmodifiedLists(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.ProportionalElectionUnmodifiedListResults>(result);
    }

    [AuthorizePermission(Permissions.ProportionalElectionListResult.Read)]
    public override async Task<ProtoModels.ProportionalElectionListResults> GetListResults(GetProportionalElectionListResultsRequest request, ServerCallContext context)
    {
        var listResults = await _proportionalElectionResultReader.GetListResults(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.ProportionalElectionListResults>(listResults);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<Empty> DefineEntry(DefineProportionalElectionResultEntryRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.DefineEntry(
            GuidParser.Parse(request.ElectionResultId),
            _mapper.Map<ProportionalElectionResultEntryParams>(request.ResultEntryParams));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<Empty> EnterCountOfVoters(EnterProportionalElectionCountOfVotersRequest request, ServerCallContext context)
    {
        var id = GuidParser.Parse(request.ElectionResultId);
        var countOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(request.CountOfVoters);
        await _proportionalElectionResultWriter.EnterCountOfVoters(id, countOfVoters);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<Empty> EnterUnmodifiedListResults(
        EnterProportionalElectionUnmodifiedListResultsRequest request,
        ServerCallContext context)
    {
        var id = GuidParser.Parse(request.ElectionResultId);
        var results = _mapper.Map<IReadOnlyCollection<ProportionalElectionUnmodifiedListResult>>(request.Results);
        await _proportionalElectionResultWriter.EnterUnmodifiedListResults(id, results);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<ProtoModels.SecondFactorTransaction> PrepareSubmissionFinished(ProportionalElectionResultPrepareSubmissionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code, qrCode) = await _proportionalElectionResultWriter.PrepareSubmissionFinished(GuidParser.Parse(request.ElectionResultId), Strings.ProportionalElectionResult_SubmissionFinished);
        return secondFactorTransaction == null ? new ProtoModels.SecondFactorTransaction() : new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.Id.ToString(), Code = code, QrCode = qrCode };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<Empty> SubmissionFinished(ProportionalElectionResultSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.SubmissionFinished(GuidParser.Parse(request.ElectionResultId), GuidParser.ParseNullable(request.SecondFactorTransactionId), context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> ResetToSubmissionFinished(ProportionalElectionResultResetToSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.ResetToSubmissionFinished(GuidParser.Parse(request.ElectionResultId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<ProtoModels.SecondFactorTransaction> PrepareCorrectionFinished(ProportionalElectionResultPrepareCorrectionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code, qrCode) = await _proportionalElectionResultWriter.PrepareCorrectionFinished(GuidParser.Parse(request.ElectionResultId), Strings.ProportionalElectionResult_CorrectionFinished);
        return secondFactorTransaction == null ? new ProtoModels.SecondFactorTransaction() : new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.Id.ToString(), Code = code, QrCode = qrCode };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<Empty> CorrectionFinished(ProportionalElectionResultCorrectionFinishedRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.CorrectionFinished(GuidParser.Parse(request.ElectionResultId), request.Comment, GuidParser.ParseNullable(request.SecondFactorTransactionId), context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> FlagForCorrection(ProportionalElectionResultFlagForCorrectionRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.FlagForCorrection(GuidParser.Parse(request.ElectionResultId), request.Comment);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> AuditedTentatively(ProportionalElectionResultAuditedTentativelyRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.AuditedTentatively(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> Plausibilise(ProportionalElectionResultsPlausibiliseRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.Plausibilise(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> ResetToAuditedTentatively(ProportionalElectionResultsResetToAuditedTentativelyRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.ResetToAuditedTentatively(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Read)]
    public override async Task<ProtoModels.ProportionalElectionEndResult> GetPartialEndResult(GetProportionalElectionPartialEndResultRequest request, ServerCallContext context)
    {
        var partialResult = await _proportionalElectionEndResultReader.GetPartialEndResult(GuidParser.Parse(request.ProportionalElectionId));

        var mapped = _mapper.Map<ProtoModels.ProportionalElectionEndResult>(partialResult);
        mapped.PartialResult = true;
        return mapped;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Read)]
    public override async Task<ProtoModels.ProportionalElectionEndResult> GetEndResult(GetProportionalElectionEndResultRequest request, ServerCallContext context)
    {
        var endResult = await _proportionalElectionEndResultReader.GetEndResult(GuidParser.Parse(request.ProportionalElectionId));
        return _mapper.Map<ProtoModels.ProportionalElectionEndResult>(endResult);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Read)]
    public override async Task<ProtoModels.DoubleProportionalResult> GetDoubleProportionalResult(GetProportionalElectionDoubleProportionalResultRequest request, ServerCallContext context)
    {
        var dpResult = await _dpResultReader.GetElectionDoubleProportionalResult(GuidParser.Parse(request.ProportionalElectionId));
        return _mapper.Map<ProtoModels.DoubleProportionalResult>(dpResult);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResultLotDecision.Read)]
    public override async Task<ProtoModels.ProportionalElectionListEndResultAvailableLotDecisions> GetListEndResultAvailableLotDecisions(
        GetProportionalElectionListEndResultAvailableLotDecisionsRequest request,
        ServerCallContext context)
    {
        var availableLotDecisions = await _proportionalElectionEndResultReader.GetEndResultAvailableLotDecisions(GuidParser.Parse(request.ProportionalElectionListId));
        return _mapper.Map<ProtoModels.ProportionalElectionListEndResultAvailableLotDecisions>(availableLotDecisions);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResultLotDecision.Update)]
    public override async Task<Empty> UpdateListEndResultLotDecisions(
        UpdateProportionalElectionListEndResultLotDecisionsRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.UpdateEndResultLotDecisions(
            GuidParser.Parse(request.ProportionalElectionListId),
            _mapper.Map<List<ElectionEndResultLotDecision>>(request.LotDecisions));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionEndResult.TriggerMandateDistribution)]
    public override async Task<Empty> StartEndResultMandateDistribution(StartProportionalElectionEndResultMandateDistributionRequest request, ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.StartMandateDistribution(GuidParser.Parse(request.ProportionalElectionId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionEndResult.TriggerMandateDistribution)]
    public override async Task<Empty> RevertEndResultMandateDistribution(RevertProportionalElectionEndResultMandateDistributionRequest request, ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.RevertMandateDistribution(GuidParser.Parse(request.ProportionalElectionId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Finalize)]
    public override async Task<ProtoModels.SecondFactorTransaction> PrepareFinalizeEndResult(PrepareFinalizeProportionalElectionEndResultRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code, qrCode) = await _proportionalElectionEndResultWriter.PrepareFinalize(GuidParser.Parse(request.ProportionalElectionId), Strings.ProportionalElectionResult_FinalizeEndResult);
        return new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.Id.ToString(), Code = code, QrCode = qrCode };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Finalize)]
    public override async Task<Empty> FinalizeEndResult(FinalizeProportionalElectionEndResultRequest request, ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.Finalize(GuidParser.Parse(request.ProportionalElectionId), GuidParser.Parse(request.SecondFactorTransactionId), context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Finalize)]
    public override async Task<Empty> RevertEndResultFinalization(RevertProportionalElectionEndResultFinalizationRequest request, ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.RevertFinalization(GuidParser.Parse(request.ProportionalElectionId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<ProtoModels.ValidationSummary> ValidateEnterCountOfVoters(ValidateEnterProportionalElectionCountOfVotersRequest request, ServerCallContext context)
    {
        var id = GuidParser.Parse(request.Request.ElectionResultId);
        var countOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(request.Request.CountOfVoters);
        var summary = await _proportionalElectionResultValidationSummaryBuilder.BuildEnterCountOfVotersValidationSummary(id, countOfVoters);
        return _mapper.Map<ProtoModels.ValidationSummary>(summary);
    }

    [AuthorizePermission(Permissions.ProportionalElectionEndResult.EnterManualResults)]
    public override async Task<Empty> EnterManualListEndResult(EnterProportionalElectionManualListEndResultRequest request, ServerCallContext context)
    {
        var listId = GuidParser.Parse(request.ProportionalElectionListId);
        var candidateEndResults = _mapper.Map<List<ProportionalElectionManualCandidateEndResult>>(request.CandidateEndResults);
        await _proportionalElectionEndResultWriter.EnterManualListEndResult(listId, candidateEndResults);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmissionAndAudit)]
    public override async Task<Empty> SubmissionFinishedAndAuditedTentatively(ProportionalElectionResultSubmissionFinishedAndAuditedTentativelyRequest request, ServerCallContext context)
    {
        var electionResultId = GuidParser.Parse(request.ElectionResultId);
        await _proportionalElectionResultWriter.SubmissionFinishedAndAuditedTentatively(electionResultId);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmissionAndAudit)]
    public override async Task<Empty> CorrectionFinishedAndAuditedTentatively(ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest request, ServerCallContext context)
    {
        var electionResultId = GuidParser.Parse(request.ElectionResultId);
        await _proportionalElectionResultWriter.CorrectionFinishedAndAuditedTentatively(electionResultId);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResultLotDecision.Read)]
    public override async Task<ProtoModels.DoubleProportionalResultSuperApportionmentAvailableLotDecisions> GetDoubleProportionalResultSuperApportionmentAvailableLotDecisions(GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest request, ServerCallContext context)
    {
        var availableLotDecisions = await _dpResultReader.GetElectionDoubleProportionalSuperApportionmentAvailableLotDecisions(GuidParser.Parse(request.ProportionalElectionId));
        return _mapper.Map<ProtoModels.DoubleProportionalResultSuperApportionmentAvailableLotDecisions>(availableLotDecisions);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResultLotDecision.Update)]
    public override async Task<Empty> UpdateDoubleProportionalResultSuperApportionmentLotDecision(UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest request, ServerCallContext context)
    {
        await _dpResultWriter.UpdateElectionSuperApportionmentLotDecision(GuidParser.Parse(request.ProportionalElectionId), request.Number);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> Publish(ProportionalElectionResultPublishRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.Publish(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> Unpublish(ProportionalElectionResultUnpublishRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.Unpublish(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResultLotDecision.Update)]
    public override async Task<Empty> UpdateEndResultListLotDecisions(
        UpdateProportionalElectionEndResultListLotDecisionsRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionEndResultWriter.UpdateEndResultListLotDecisions(
            GuidParser.Parse(request.ProportionalElectionId),
            _mapper.Map<List<ProportionalElectionEndResultListLotDecision>>(request.ListLotDecisions));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> ResetToSubmissionFinishedAndFlagForCorrection(ProportionalElectionResultResetToSubmissionFinishedAndFlagForCorrectionRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultWriter.ResetToSubmissionFinishedAndFlagForCorrection(GuidParser.Parse(request.ElectionResultId));
        return ProtobufEmpty.Instance;
    }
}
