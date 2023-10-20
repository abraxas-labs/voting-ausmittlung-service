// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Ausmittlung.Resources;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ResultService.ResultServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class ResultService : ServiceBase
{
    private readonly ResultReader _resultReader;
    private readonly ResultWriter _resultWriter;
    private readonly IMapper _mapper;

    public ResultService(
        ResultReader resultReader,
        ResultWriter resultWriter,
        IMapper mapper)
    {
        _resultReader = resultReader;
        _resultWriter = resultWriter;
        _mapper = mapper;
    }

    public override async Task<ProtoModels.ResultOverview> GetOverview(
        GetResultOverviewRequest request,
        ServerCallContext context)
    {
        var data = await _resultReader.GetResultOverview(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.ResultOverview>(data);
    }

    public override async Task<ProtoModels.ResultList> GetList(
        GetResultListRequest request,
        ServerCallContext context)
    {
        var data = await _resultReader.GetList(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId));

        // If a user accesses the result list, start the submission (if it is not already started)
        await _resultWriter.StartSubmission(data);

        return _mapper.Map<ProtoModels.ResultList>(data);
    }

    public override async Task<ProtoModels.Comments> GetResultComments(
        GetResultCommentsRequest request,
        ServerCallContext context)
    {
        var comments = await _resultReader.GetComments(GuidParser.Parse(request.ResultId));
        return _mapper.Map<ProtoModels.Comments>(comments);
    }

    public override Task GetStateChanges(
        GetResultStateChangesRequest request,
        IServerStreamWriter<ProtoModels.ResultStateChange> responseStream,
        ServerCallContext context)
    {
        return _resultReader.ListenToResultStateChanges(
            GuidParser.Parse(request.ContestId),
            e => responseStream.WriteAsync(new ProtoModels.ResultStateChange
            {
                Id = e.Id.ToString(),
                NewState = _mapper.Map<ProtoModels.CountingCircleResultState>(e.NewState),
            }),
            context.CancellationToken);
    }

    public override async Task<Empty> ResetCountingCircleResults(ResetCountingCircleResultsRequest request, ServerCallContext context)
    {
        await _resultWriter.ResetResults(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId));

        return ProtobufEmpty.Instance;
    }

    public override async Task<ValidationSummaries> ValidateCountingCircleResults(ValidateCountingCircleResultsRequest request, ServerCallContext context)
    {
        var pbValidationResults = await _resultReader.GetCountingCircleResultsValidationResults(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId),
            request.CountingCircleResultIds.Select(GuidParser.Parse).ToList());

        return _mapper.Map<ValidationSummaries>(pbValidationResults);
    }

    public override async Task<SecondFactorTransaction> PrepareSubmissionFinished(CountingCircleResultsPrepareSubmissionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code) = await _resultWriter.PrepareSubmissionFinished(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId),
            request.CountingCircleResultIds.Select(GuidParser.Parse).ToList(),
            Strings.CountingCircleResults_SubmissionFinished);
        return secondFactorTransaction == null ? new SecondFactorTransaction() : new SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code };
    }

    public override async Task<Empty> SubmissionFinished(CountingCircleResultsSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _resultWriter.SubmissionFinished(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId),
            request.CountingCircleResultIds.Select(GuidParser.Parse).ToList(),
            request.SecondFactorTransactionId,
            context.CancellationToken);
        return ProtobufEmpty.Instance;
    }
}
