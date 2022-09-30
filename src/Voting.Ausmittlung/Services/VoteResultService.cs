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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Resources;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.VoteResultService.VoteResultServiceBase;
using VoteResultEntryParams = Voting.Ausmittlung.Core.Domain.VoteResultEntryParams;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class VoteResultService : ServiceBase
{
    private readonly VoteResultReader _voteResultReader;
    private readonly BallotResultReader _ballotResultReader;
    private readonly VoteEndResultReader _voteEndResultReader;
    private readonly VoteResultWriter _voteResultWriter;
    private readonly VoteEndResultWriter _voteEndResultWriter;
    private readonly IMapper _mapper;
    private readonly VoteResultValidationResultsBuilder _voteResultValidationResultsBuilder;

    public VoteResultService(
        IMapper mapper,
        VoteResultReader voteResultReader,
        BallotResultReader ballotResultReader,
        VoteEndResultReader voteEndResultReader,
        VoteResultWriter voteResultWriter,
        VoteEndResultWriter voteEndResultWriter,
        VoteResultValidationResultsBuilder voteResultValidationResultsBuilder)
    {
        _mapper = mapper;
        _voteResultReader = voteResultReader;
        _ballotResultReader = ballotResultReader;
        _voteEndResultReader = voteEndResultReader;
        _voteResultWriter = voteResultWriter;
        _voteEndResultWriter = voteEndResultWriter;
        _voteResultValidationResultsBuilder = voteResultValidationResultsBuilder;
    }

    public override async Task<ProtoModels.VoteResult> Get(GetVoteResultRequest request, ServerCallContext context)
    {
        var result = string.IsNullOrEmpty(request.VoteResultId)
            ? await _voteResultReader.Get(GuidParser.Parse(request.VoteId), GuidParser.Parse(request.CountingCircleId))
            : await _voteResultReader.Get(GuidParser.Parse(request.VoteResultId));
        return _mapper.Map<ProtoModels.VoteResult>(result);
    }

    public override async Task<Empty> DefineEntry(DefineVoteResultEntryRequest request, ServerCallContext context)
    {
        await _voteResultWriter.DefineEntry(
            GuidParser.Parse(request.VoteResultId),
            _mapper.Map<VoteResultEntry>(request.ResultEntry),
            _mapper.Map<VoteResultEntryParams>(request.ResultEntryParams));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> EnterCountOfVoters(EnterVoteResultCountOfVotersRequest request, ServerCallContext context)
    {
        var resultsCountOfVoters = _mapper.Map<List<VoteBallotResultsCountOfVoters>>(request.ResultsCountOfVoters);
        await _voteResultWriter.EnterCountOfVoters(GuidParser.Parse(request.VoteResultId), resultsCountOfVoters);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> EnterResults(EnterVoteResultsRequest request, ServerCallContext context)
    {
        var ballotResults = _mapper.Map<List<VoteBallotResults>>(request.Results);
        await _voteResultWriter.EnterResults(GuidParser.Parse(request.VoteResultId), ballotResults);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> EnterCorrectionResults(EnterVoteResultCorrectionRequest request, ServerCallContext context)
    {
        var ballotResults = _mapper.Map<List<VoteBallotResults>>(request.Results);
        await _voteResultWriter.EnterCorrectionResults(GuidParser.Parse(request.VoteResultId), ballotResults);
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.SecondFactorTransaction> PrepareSubmissionFinished(VoteResultPrepareSubmissionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _voteResultWriter.PrepareSubmissionFinished(GuidParser.Parse(request.VoteResultId), Strings.VoteResult_SubmissionFinished);
        return new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    public override async Task<Empty> SubmissionFinished(VoteResultSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _voteResultWriter.SubmissionFinished(GuidParser.Parse(request.VoteResultId), request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> ResetToSubmissionFinished(VoteResultResetToSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _voteResultWriter.ResetToSubmissionFinished(GuidParser.Parse(request.VoteResultId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.SecondFactorTransaction> PrepareCorrectionFinished(VoteResultPrepareCorrectionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _voteResultWriter.PrepareCorrectionFinished(GuidParser.Parse(request.VoteResultId), Strings.VoteResult_CorrectionFinished);
        return new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    public override async Task<Empty> CorrectionFinished(VoteResultCorrectionFinishedRequest request, ServerCallContext context)
    {
        await _voteResultWriter.CorrectionFinished(GuidParser.Parse(request.VoteResultId), request.Comment, request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> FlagForCorrection(VoteResultFlagForCorrectionRequest request, ServerCallContext context)
    {
        await _voteResultWriter.FlagForCorrection(GuidParser.Parse(request.VoteResultId), request.Comment);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> AuditedTentatively(VoteResultAuditedTentativelyRequest request, ServerCallContext context)
    {
        await _voteResultWriter.AuditedTentatively(request.VoteResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> Plausibilise(VoteResultsPlausibiliseRequest request, ServerCallContext context)
    {
        await _voteResultWriter.Plausibilise(request.VoteResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> ResetToAuditedTentatively(VoteResultsResetToAuditedTentativelyRequest request, ServerCallContext context)
    {
        await _voteResultWriter.ResetToAuditedTentatively(request.VoteResultIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.VoteEndResult> GetEndResult(GetVoteEndResultRequest request, ServerCallContext context)
    {
        var result = await _voteEndResultReader.GetEndResult(GuidParser.Parse(request.VoteId));
        return _mapper.Map<ProtoModels.VoteEndResult>(result);
    }

    public override async Task<ProtoModels.SecondFactorTransaction> PrepareFinalizeEndResult(PrepareFinalizeVoteEndResultRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _voteEndResultWriter.PrepareFinalize(GuidParser.Parse(request.VoteId), Strings.VoteResult_FinalizeEndResult);
        return new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    public override async Task<Empty> FinalizeEndResult(FinalizeVoteEndResultRequest request, ServerCallContext context)
    {
        await _voteEndResultWriter.Finalize(GuidParser.Parse(request.VoteId), request.SecondFactorTransactionId, context.CancellationToken);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> RevertEndResultFinalization(RevertVoteEndResultFinalizationRequest request, ServerCallContext context)
    {
        await _voteEndResultWriter.RevertFinalization(GuidParser.Parse(request.VoteId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.BallotResult> GetBallotResult(GetBallotResultRequest request, ServerCallContext context)
    {
        var result = await _ballotResultReader.Get(GuidParser.Parse(request.BallotResultId));
        return _mapper.Map<ProtoModels.BallotResult>(result);
    }

    public override async Task<ProtoModels.ValidationOverview> ValidateEnterCountOfVoters(ValidateEnterVoteResultCountOfVotersRequest request, ServerCallContext context)
    {
        var id = GuidParser.Parse(request.Request.VoteResultId);
        var countOfVoters = _mapper.Map<List<VoteBallotResultsCountOfVoters>>(request.Request.ResultsCountOfVoters);
        var results = await _voteResultValidationResultsBuilder.BuildEnterCountOfVotersValidationResults(id, countOfVoters);
        return _mapper.Map<ProtoModels.ValidationOverview>(results);
    }

    public override async Task<ProtoModels.ValidationOverview> ValidateEnterResults(ValidateEnterVoteResultsRequest request, ServerCallContext context)
    {
        var id = GuidParser.Parse(request.Request.VoteResultId);
        var results = _mapper.Map<List<VoteBallotResults>>(request.Request.Results);
        var validationResults = await _voteResultValidationResultsBuilder.BuildEnterResultsValidationResults(id, results);
        return _mapper.Map<ProtoModels.ValidationOverview>(validationResults);
    }

    public override async Task<ProtoModels.ValidationOverview> ValidateEnterCorrectionResults(ValidateEnterVoteResultCorrectionRequest request, ServerCallContext context)
    {
        var id = GuidParser.Parse(request.Request.VoteResultId);
        var results = _mapper.Map<List<VoteBallotResults>>(request.Request.Results);
        var validationResults = await _voteResultValidationResultsBuilder.BuildEnterResultsValidationResults(id, results);
        return _mapper.Map<ProtoModels.ValidationOverview>(validationResults);
    }
}
