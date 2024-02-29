// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Ausmittlung.Resources;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.MajorityElectionResultService.MajorityElectionResultServiceBase;

namespace Voting.Ausmittlung.Services;

public class MajorityElectionResultService : ServiceBase
{
    private readonly MajorityElectionResultReader _majorityElectionResultReader;
    private readonly MajorityElectionResultWriter _majorityElectionResultWriter;
    private readonly MajorityElectionEndResultReader _majorityElectionEndResultReader;
    private readonly MajorityElectionEndResultWriter _majorityElectionEndResultWriter;
    private readonly MajorityElectionResultValidationSummaryBuilder _majorityElectionResultValidationSummaryBuilder;
    private readonly IMapper _mapper;

    public MajorityElectionResultService(
        MajorityElectionResultReader majorityElectionResultReader,
        MajorityElectionResultWriter majorityElectionResultWriter,
        MajorityElectionEndResultReader majorityElectionEndResultReader,
        MajorityElectionEndResultWriter majorityElectionEndResultWriter,
        MajorityElectionResultValidationSummaryBuilder majorityElectionResultValidationSummaryBuilder,
        IMapper mapper)
    {
        _majorityElectionResultReader = majorityElectionResultReader;
        _majorityElectionResultWriter = majorityElectionResultWriter;
        _majorityElectionEndResultReader = majorityElectionEndResultReader;
        _majorityElectionEndResultWriter = majorityElectionEndResultWriter;
        _majorityElectionResultValidationSummaryBuilder = majorityElectionResultValidationSummaryBuilder;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Read)]
    public override async Task<ProtoModels.MajorityElectionResult> Get(GetMajorityElectionResultRequest request, ServerCallContext context)
    {
        var result = string.IsNullOrEmpty(request.ElectionResultId)
            ? await _majorityElectionResultReader.Get(GuidParser.Parse(request.ElectionId), GuidParser.Parse(request.CountingCircleId))
            : await _majorityElectionResultReader.Get(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.MajorityElectionResult>(result);
    }

    [AuthorizePermission(Permissions.MajorityElectionBallotGroupResult.Read)]
    public override async Task<ProtoModels.MajorityElectionBallotGroupResults> GetBallotGroups(
        GetMajorityElectionBallotGroupResultsRequest request,
        ServerCallContext context)
    {
        var result = await _majorityElectionResultReader.GetWithBallotGroups(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.MajorityElectionBallotGroupResults>(result);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<Empty> DefineEntry(DefineMajorityElectionResultEntryRequest request, ServerCallContext context)
    {
        await _majorityElectionResultWriter.DefineEntry(
            GuidParser.Parse(request.ElectionResultId),
            _mapper.Map<DataModels.MajorityElectionResultEntry>(request.ResultEntry),
            _mapper.Map<MajorityElectionResultEntryParams>(request.ResultEntryParams));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<Empty> EnterCountOfVoters(EnterMajorityElectionCountOfVotersRequest request, ServerCallContext context)
    {
        if (request.CountOfVoters == null)
        {
            throw new ValidationException("count of voters is required");
        }

        var countOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(request.CountOfVoters);
        await _majorityElectionResultWriter.EnterCountOfVoters(GuidParser.Parse(request.ElectionResultId), countOfVoters);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<Empty> EnterCandidateResults(EnterMajorityElectionCandidateResultsRequest request, ServerCallContext context)
    {
        var countOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(request.CountOfVoters);
        var candidateResults = _mapper.Map<List<MajorityElectionCandidateResult>>(request.CandidateResults);
        var secondaryCandidateResults = _mapper.Map<List<SecondaryMajorityElectionCandidateResults>>(request.SecondaryElectionCandidateResults);
        await _majorityElectionResultWriter.EnterCandidateResults(
            GuidParser.Parse(request.ElectionResultId),
            request.IndividualVoteCount,
            request.EmptyVoteCount,
            request.InvalidVoteCount,
            countOfVoters,
            candidateResults,
            secondaryCandidateResults);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<Empty> EnterBallotGroupResults(EnterMajorityElectionBallotGroupResultsRequest request, ServerCallContext context)
    {
        var results = _mapper.Map<List<MajorityElectionBallotGroupResult>>(request.Results);
        await _majorityElectionResultWriter.EnterBallotGroupResults(
            GuidParser.Parse(request.ElectionResultId),
            results);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<ProtoModels.SecondFactorTransaction> PrepareSubmissionFinished(MajorityElectionResultPrepareSubmissionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _majorityElectionResultWriter.PrepareSubmissionFinished(GuidParser.Parse(request.ElectionResultId), Strings.MajorityElectionResult_SubmissionFinished);
        return secondFactorTransaction == null ? new ProtoModels.SecondFactorTransaction() : new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<Empty> SubmissionFinished(MajorityElectionResultSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _majorityElectionResultWriter.SubmissionFinished(GuidParser.Parse(request.ElectionResultId), request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> ResetToSubmissionFinished(MajorityElectionResultResetToSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _majorityElectionResultWriter.ResetToSubmissionFinished(GuidParser.Parse(request.ElectionResultId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<ProtoModels.SecondFactorTransaction> PrepareCorrectionFinished(MajorityElectionResultPrepareCorrectionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _majorityElectionResultWriter.PrepareCorrectionFinished(GuidParser.Parse(request.ElectionResultId), Strings.MajorityElectionResult_CorrectionFinished);
        return secondFactorTransaction == null ? new ProtoModels.SecondFactorTransaction() : new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<Empty> CorrectionFinished(MajorityElectionResultCorrectionFinishedRequest request, ServerCallContext context)
    {
        await _majorityElectionResultWriter.CorrectionFinished(GuidParser.Parse(request.ElectionResultId), request.Comment, request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> FlagForCorrection(MajorityElectionResultFlagForCorrectionRequest request, ServerCallContext context)
    {
        await _majorityElectionResultWriter.FlagForCorrection(GuidParser.Parse(request.ElectionResultId), request.Comment);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> AuditedTentatively(MajorityElectionResultAuditedTentativelyRequest request, ServerCallContext context)
    {
        await _majorityElectionResultWriter.AuditedTentatively(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> Plausibilise(MajorityElectionResultsPlausibiliseRequest request, ServerCallContext context)
    {
        await _majorityElectionResultWriter.Plausibilise(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Audit)]
    public override async Task<Empty> ResetToAuditedTentatively(MajorityElectionResultsResetToAuditedTentativelyRequest request, ServerCallContext context)
    {
        await _majorityElectionResultWriter.ResetToAuditedTentatively(request.ElectionResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Read)]
    public override async Task<ProtoModels.MajorityElectionEndResult> GetEndResult(GetMajorityElectionEndResultRequest request, ServerCallContext context)
    {
        var endResult = await _majorityElectionEndResultReader.GetEndResult(GuidParser.Parse(request.MajorityElectionId));
        return _mapper.Map<ProtoModels.MajorityElectionEndResult>(endResult);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResultLotDecision.Read)]
    public override async Task<ProtoModels.MajorityElectionEndResultAvailableLotDecisions> GetEndResultAvailableLotDecisions(GetMajorityElectionEndResultAvailableLotDecisionsRequest request, ServerCallContext context)
    {
        var availableLotDecisions = await _majorityElectionEndResultReader.GetEndResultAvailableLotDecisions(GuidParser.Parse(request.MajorityElectionId));
        return _mapper.Map<ProtoModels.MajorityElectionEndResultAvailableLotDecisions>(availableLotDecisions);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResultLotDecision.Update)]
    public override async Task<Empty> UpdateEndResultLotDecisions(UpdateMajorityElectionEndResultLotDecisionsRequest request, ServerCallContext context)
    {
        await _majorityElectionEndResultWriter.UpdateEndResultLotDecisions(
            GuidParser.Parse(request.MajorityElectionId),
            _mapper.Map<List<ElectionEndResultLotDecision>>(request.LotDecisions));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Finalize)]
    public override async Task<ProtoModels.SecondFactorTransaction> PrepareFinalizeEndResult(PrepareFinalizeMajorityElectionEndResultRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _majorityElectionEndResultWriter.PrepareFinalize(GuidParser.Parse(request.MajorityElectionId), Strings.MajorityElectionResult_FinalizeEndResult);
        return new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Finalize)]
    public override async Task<Empty> FinalizeEndResult(FinalizeMajorityElectionEndResultRequest request, ServerCallContext context)
    {
        await _majorityElectionEndResultWriter.Finalize(GuidParser.Parse(request.MajorityElectionId), request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessEndResult.Finalize)]
    public override async Task<Empty> RevertEndResultFinalization(RevertMajorityElectionEndResultFinalizationRequest request, ServerCallContext context)
    {
        await _majorityElectionEndResultWriter.RevertFinalization(GuidParser.Parse(request.MajorityElectionId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<ProtoModels.ValidationSummary> ValidateEnterCountOfVoters(ValidateEnterMajorityElectionCountOfVotersRequest request, ServerCallContext context)
    {
        var electionResultId = GuidParser.Parse(request.Request.ElectionResultId);
        var countOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(request.Request.CountOfVoters);
        var results = await _majorityElectionResultValidationSummaryBuilder.BuildEnterCountOfVotersValidationSummary(
                electionResultId,
                countOfVoters);
        return _mapper.Map<ProtoModels.ValidationSummary>(results);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<ProtoModels.ValidationSummary> ValidateEnterCandidateResults(ValidateEnterMajorityElectionCandidateResultsRequest request, ServerCallContext context)
    {
        var electionResultId = GuidParser.Parse(request.Request.ElectionResultId);
        var countOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(request.Request.CountOfVoters);
        var candidateResults = _mapper.Map<IReadOnlyCollection<MajorityElectionCandidateResult>>(request.Request.CandidateResults);
        var secondaryResults = _mapper.Map<IReadOnlyCollection<SecondaryMajorityElectionCandidateResults>>(request.Request.SecondaryElectionCandidateResults);
        var summary = await _majorityElectionResultValidationSummaryBuilder.BuildEnterCandidateResultsValidationSummary(
            electionResultId,
            countOfVoters,
            request.Request.IndividualVoteCount,
            request.Request.EmptyVoteCount,
            request.Request.InvalidVoteCount,
            candidateResults,
            secondaryResults);
        return _mapper.Map<ProtoModels.ValidationSummary>(summary);
    }
}
