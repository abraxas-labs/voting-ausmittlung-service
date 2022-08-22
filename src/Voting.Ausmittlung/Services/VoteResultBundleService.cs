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
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using DomainModels = Voting.Ausmittlung.Core.Domain;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.VoteResultBundleService.VoteResultBundleServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class VoteResultBundleService : ServiceBase
{
    private readonly VoteResultBundleReader _voteResultBundleReader;
    private readonly VoteResultBundleWriter _voteResultBundleWriter;
    private readonly IMapper _mapper;

    public VoteResultBundleService(
        VoteResultBundleReader voteResultBundleReader,
        VoteResultBundleWriter voteResultBundleWriter,
        IMapper mapper)
    {
        _voteResultBundleReader = voteResultBundleReader;
        _voteResultBundleWriter = voteResultBundleWriter;
        _mapper = mapper;
    }

    public override async Task<ProtoModels.VoteResultBundles> GetBundles(
        GetVoteResultBundlesRequest request,
        ServerCallContext context)
    {
        var ballotResult = await _voteResultBundleReader.GetBallotResultWithBundles(GuidParser.Parse(request.BallotResultId));
        return _mapper.Map<ProtoModels.VoteResultBundles>(ballotResult);
    }

    public override Task GetBundleChanges(
        GetVoteResultBundleChangesRequest request,
        IServerStreamWriter<ProtoModels.VoteResultBundle> responseStream,
        ServerCallContext context)
    {
        return _voteResultBundleReader.ListenToBundleChanges(
            GuidParser.Parse(request.BallotResultId),
            b => responseStream.WriteAsync(_mapper.Map<ProtoModels.VoteResultBundle>(b)),
            context.CancellationToken);
    }

    public override async Task<GetVoteResultBundleResponse> GetBundle(
        GetVoteResultBundleRequest request,
        ServerCallContext context)
    {
        var bundle = await _voteResultBundleReader.GetBundle(GuidParser.Parse(request.BundleId));
        return _mapper.Map<GetVoteResultBundleResponse>(bundle);
    }

    public override async Task<ProtoModels.VoteResultBallot> GetBallot(
        GetVoteResultBallotRequest request,
        ServerCallContext context)
    {
        var ballot = await _voteResultBundleReader.GetBallot(GuidParser.Parse(request.BundleId), request.BallotNumber);
        return _mapper.Map<ProtoModels.VoteResultBallot>(ballot);
    }

    public override async Task<CreateVoteResultBundleResponse> CreateBundle(
        CreateVoteResultBundleRequest request,
        ServerCallContext context)
    {
        var response = await _voteResultBundleWriter.CreateBundle(
            GuidParser.Parse(request.VoteResultId),
            GuidParser.Parse(request.BallotResultId),
            request.BundleNumber);

        return new()
        {
            BundleId = response.Id.ToString(),
            BundleNumber = response.BundleNumber,
        };
    }

    public override async Task<Empty> DeleteBundle(
        DeleteVoteResultBundleRequest request,
        ServerCallContext context)
    {
        await _voteResultBundleWriter.DeleteBundle(GuidParser.Parse(request.BundleId), GuidParser.Parse(request.BallotResultId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<CreateVoteResultBallotResponse> CreateBallot(
        CreateVoteResultBallotRequest request,
        ServerCallContext context)
    {
        var questionAnswers = _mapper.Map<List<DomainModels.VoteResultBallotQuestionAnswer>>(request.QuestionAnswers);
        var tieBreakQuestionAnswers = _mapper.Map<List<DomainModels.VoteResultBallotTieBreakQuestionAnswer>>(request.TieBreakQuestionAnswers);
        var ballotNumber = await _voteResultBundleWriter.CreateBallot(
            GuidParser.Parse(request.BundleId),
            questionAnswers,
            tieBreakQuestionAnswers);
        return new CreateVoteResultBallotResponse
        {
            BundleId = request.BundleId,
            BallotNumber = ballotNumber,
        };
    }

    public override async Task<Empty> UpdateBallot(
        UpdateVoteResultBallotRequest request,
        ServerCallContext context)
    {
        var questionAnswers = _mapper.Map<List<DomainModels.VoteResultBallotQuestionAnswer>>(request.QuestionAnswers);
        var tieBreakQuestionAnswers = _mapper.Map<List<DomainModels.VoteResultBallotTieBreakQuestionAnswer>>(request.TieBreakQuestionAnswers);
        await _voteResultBundleWriter.UpdateBallot(
            GuidParser.Parse(request.BundleId),
            request.BallotNumber,
            questionAnswers,
            tieBreakQuestionAnswers);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> DeleteBallot(
        DeleteVoteResultBallotRequest request,
        ServerCallContext context)
    {
        await _voteResultBundleWriter.DeleteBallot(GuidParser.Parse(request.BundleId), request.BallotNumber);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> BundleSubmissionFinished(
        VoteResultBundleSubmissionFinishedRequest request,
        ServerCallContext context)
    {
        await _voteResultBundleWriter.BundleSubmissionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> BundleCorrectionFinished(
        VoteResultBundleCorrectionFinishedRequest request,
        ServerCallContext context)
    {
        await _voteResultBundleWriter.BundleCorrectionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> RejectBundleReview(RejectVoteBundleReviewRequest request, ServerCallContext context)
    {
        await _voteResultBundleWriter.RejectBundleReview(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> SucceedBundleReview(SucceedVoteBundleReviewRequest request, ServerCallContext context)
    {
        await _voteResultBundleWriter.SucceedBundleReview(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }
}
