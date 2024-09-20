// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
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
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ResultService.ResultServiceBase;

namespace Voting.Ausmittlung.Services;

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

    [AuthorizePermission(Permissions.PoliticalBusinessResult.ReadOverview)]
    public override async Task<ProtoModels.ResultOverview> GetOverview(
        GetResultOverviewRequest request,
        ServerCallContext context)
    {
        var data = await _resultReader.GetResultOverview(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.ResultOverview>(data);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Read)]
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

    [AuthorizePermission(Permissions.PoliticalBusinessResult.ReadComments)]
    public override async Task<ProtoModels.Comments> GetResultComments(
        GetResultCommentsRequest request,
        ServerCallContext context)
    {
        var comments = await _resultReader.GetComments(GuidParser.Parse(request.ResultId));
        return _mapper.Map<ProtoModels.Comments>(comments);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.Read)]
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

    [AuthorizePermission(Permissions.PoliticalBusinessResult.ResetResults)]
    public override async Task<Empty> ResetCountingCircleResults(ResetCountingCircleResultsRequest request, ServerCallContext context)
    {
        await _resultWriter.ResetResults(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId));

        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.EnterResults)]
    public override async Task<ProtoModels.ValidationSummaries> ValidateCountingCircleResults(ValidateCountingCircleResultsRequest request, ServerCallContext context)
    {
        var pbValidationResults = await _resultReader.GetCountingCircleResultsValidationResults(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId),
            request.CountingCircleResultIds.Select(GuidParser.Parse).ToList());

        return _mapper.Map<ProtoModels.ValidationSummaries>(pbValidationResults);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
    public override async Task<ProtoModels.SecondFactorTransaction> PrepareSubmissionFinished(CountingCircleResultsPrepareSubmissionFinishedRequest request, ServerCallContext context)
    {
        var (secondFactorTransaction, code, qrCode) = await _resultWriter.PrepareSubmissionFinished(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId),
            request.CountingCircleResultIds.Select(GuidParser.Parse).ToList(),
            Strings.CountingCircleResults_SubmissionFinished);
        return secondFactorTransaction == null
            ? new ProtoModels.SecondFactorTransaction()
            : new ProtoModels.SecondFactorTransaction { Id = secondFactorTransaction.ExternalIdentifier, Code = code, QrCode = qrCode };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResult.FinishSubmission)]
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
