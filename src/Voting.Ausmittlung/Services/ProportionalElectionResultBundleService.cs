// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Services.V1.Responses;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceBase;

namespace Voting.Ausmittlung.Services;

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

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Read)]
    public override async Task<ProtoModels.ProportionalElectionResultBundles> GetBundles(
        GetProportionalElectionResultBundlesRequest request,
        ServerCallContext context)
    {
        var electionResult = await _proportionalElectionResultBundleReader.GetElectionResultWithBundles(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.ProportionalElectionResultBundles>(electionResult);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Read)]
    public override async Task<GetProportionalElectionResultBundleResponse> GetBundle(
        GetProportionalElectionResultBundleRequest request,
        ServerCallContext context)
    {
        var bundle = await _proportionalElectionResultBundleReader.GetBundle(GuidParser.Parse(request.BundleId));
        return _mapper.Map<GetProportionalElectionResultBundleResponse>(bundle);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBallot.Read)]
    public override async Task<ProtoModels.ProportionalElectionResultBallot> GetBallot(
        GetProportionalElectionResultBallotRequest request,
        ServerCallContext context)
    {
        var ballot = await _proportionalElectionResultBundleReader.GetBallot(GuidParser.Parse(request.BundleId), request.BallotNumber);
        return _mapper.Map<ProtoModels.ProportionalElectionResultBallot>(ballot);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Create)]
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

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Delete)]
    public override async Task<Empty> DeleteBundle(
        DeleteProportionalElectionResultBundleRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.DeleteBundle(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBallot.Create)]
    public override async Task<CreateProportionalElectionResultBallotResponse> CreateBallot(
        CreateProportionalElectionResultBallotRequest request,
        ServerCallContext context)
    {
        var candidateBallots = _mapper.Map<List<ProportionalElectionResultBallotCandidate>>(request.Candidates);
        var ballotNumber = await _proportionalElectionResultBundleWriter.CreateBallot(
            GuidParser.Parse(request.BundleId),
            request.BallotNumber,
            request.EmptyVoteCount,
            candidateBallots);
        return new CreateProportionalElectionResultBallotResponse
        {
            BundleId = request.BundleId,
            BallotNumber = ballotNumber,
        };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBallot.Update)]
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

    [AuthorizePermission(Permissions.PoliticalBusinessResultBallot.Delete)]
    public override async Task<Empty> DeleteBallot(
        DeleteProportionalElectionResultBallotRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.DeleteBallot(GuidParser.Parse(request.BundleId), request.BallotNumber);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.FinishSubmission)]
    public override async Task<Empty> BundleSubmissionFinished(
        ProportionalElectionResultBundleSubmissionFinishedRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.BundleSubmissionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.FinishSubmission)]
    public override async Task<Empty> BundleCorrectionFinished(
        ProportionalElectionResultBundleCorrectionFinishedRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.BundleCorrectionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Review)]
    public override async Task<Empty> RejectBundleReview(RejectProportionalElectionBundleReviewRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.RejectBundleReview(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Review)]
    public override async Task<Empty> SucceedBundleReview(SucceedProportionalElectionBundleReviewRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.SucceedBundleReview(request.BundleIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.ResetToSubmissionFinished)]
    public override async Task<Empty> BundleResetToSubmissionFinished(ProportionalElectionResultBundleResetToSubmissionFinishedRequest request, ServerCallContext context)
    {
        await _proportionalElectionResultBundleWriter.ResetToSubmissionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }
}
