// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Services.V1.Responses;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class ProportionalElectionResultBundleService : ServiceBase
{
    private readonly ProportionalElectionResultBundleReader _proportionalElectionResultBundleReader;
    private readonly ProportionalElectionResultBundleWriter _proportionalElectionResultBundleWriter;
    private readonly IMapper _mapper;

    public ProportionalElectionResultBundleService(
        ProportionalElectionResultBundleReader proportionalElectionResultBundleReader,
        ProportionalElectionResultBundleWriter proportionalElectionResultBundleWriter,
        IMapper mapper)
    {
        _proportionalElectionResultBundleReader = proportionalElectionResultBundleReader;
        _proportionalElectionResultBundleWriter = proportionalElectionResultBundleWriter;
        _mapper = mapper;
    }

    public override async Task<ProtoModels.ProportionalElectionResultBundles> GetBundles(
        GetProportionalElectionResultBundlesRequest request,
        ServerCallContext context)
    {
        var electionResult = await _proportionalElectionResultBundleReader.GetElectionResultWithBundles(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.ProportionalElectionResultBundles>(electionResult);
    }

    public override async Task<GetProportionalElectionResultBundleResponse> GetBundle(
        GetProportionalElectionResultBundleRequest request,
        ServerCallContext context)
    {
        var bundle = await _proportionalElectionResultBundleReader.GetBundle(GuidParser.Parse(request.BundleId));
        return _mapper.Map<GetProportionalElectionResultBundleResponse>(bundle);
    }

    public override Task GetBundleChanges(
        GetProportionalElectionResultBundleChangesRequest request,
        IServerStreamWriter<ProtoModels.ProportionalElectionResultBundle> responseStream,
        ServerCallContext context)
    {
        return _proportionalElectionResultBundleReader.ListenToBundleChanges(
            GuidParser.Parse(request.ElectionResultId),
            b => responseStream.WriteAsync(_mapper.Map<ProtoModels.ProportionalElectionResultBundle>(b)),
            context.CancellationToken);
    }

    public override async Task<ProtoModels.ProportionalElectionResultBallot> GetBallot(
        GetProportionalElectionResultBallotRequest request,
        ServerCallContext context)
    {
        var ballot = await _proportionalElectionResultBundleReader.GetBallot(GuidParser.Parse(request.BundleId), request.BallotNumber);
        return _mapper.Map<ProtoModels.ProportionalElectionResultBallot>(ballot);
    }

    public override async Task<CreateProportionalElectionResultBundleResponse> CreateBundle(CreateProportionalElectionResultBundleRequest request, ServerCallContext context)
    {
        var response = await _proportionalElectionResultBundleWriter.CreateBundle(
            GuidParser.Parse(request.ElectionResultId),
            GuidParser.ParseNullable(request.ListId),
            request.BundleNumber);
        return new()
        {
            BundleId = response.Id.ToString(),
            BundleNumber = response.BundleNumber,
        };
    }

    public override async Task<Empty> DeleteBundle(
        DeleteProportionalElectionResultBundleRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.DeleteBundle(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<CreateProportionalElectionResultBallotResponse> CreateBallot(
        CreateProportionalElectionResultBallotRequest request,
        ServerCallContext context)
    {
        var candidateBallots = _mapper.Map<List<ProportionalElectionResultBallotCandidate>>(request.Candidates);
        var ballotNumber = await _proportionalElectionResultBundleWriter.CreateBallot(
            GuidParser.Parse(request.BundleId),
            request.EmptyVoteCount,
            candidateBallots);
        return new CreateProportionalElectionResultBallotResponse
        {
            BundleId = request.BundleId,
            BallotNumber = ballotNumber,
        };
    }

    public override async Task<Empty> UpdateBallot(
        UpdateProportionalElectionResultBallotRequest request,
        ServerCallContext context)
    {
        var candidateBallots = _mapper.Map<List<ProportionalElectionResultBallotCandidate>>(request.Candidates);
        await _proportionalElectionResultBundleWriter.UpdateBallot(
            GuidParser.Parse(request.BundleId),
            request.BallotNumber,
            request.EmptyVoteCount,
            candidateBallots);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> DeleteBallot(
        DeleteProportionalElectionResultBallotRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.DeleteBallot(GuidParser.Parse(request.BundleId), request.BallotNumber);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> BundleSubmissionFinished(
        ProportionalElectionResultBundleSubmissionFinishedRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.BundleSubmissionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> BundleCorrectionFinished(
        ProportionalElectionResultBundleCorrectionFinishedRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.BundleCorrectionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> RejectBundleReview(RejectProportionalElectionBundleReviewRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.RejectBundleReview(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> SucceedBundleReview(SucceedProportionalElectionBundleReviewRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.SucceedBundleReview(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }
}
