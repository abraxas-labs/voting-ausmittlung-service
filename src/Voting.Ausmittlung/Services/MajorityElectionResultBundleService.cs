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
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.MajorityElectionResultBundleService.MajorityElectionResultBundleServiceBase;

namespace Voting.Ausmittlung.Services;

public class MajorityElectionResultBundleService : ServiceBase
{
    private readonly MajorityElectionResultBundleReader _majorityElectionResultBundleReader;
    private readonly MajorityElectionResultBundleWriter _majorityElectionResultBundleWriter;
    private readonly IMapper _mapper;

    public MajorityElectionResultBundleService(
        MajorityElectionResultBundleReader majorityElectionResultBundleReader,
        MajorityElectionResultBundleWriter majorityElectionResultBundleWriter,
        IMapper mapper)
    {
        _majorityElectionResultBundleReader = majorityElectionResultBundleReader;
        _majorityElectionResultBundleWriter = majorityElectionResultBundleWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Read)]
    public override async Task<ProtoModels.MajorityElectionResultBundles> GetBundles(
        GetMajorityElectionResultBundlesRequest request,
        ServerCallContext context)
    {
        var electionResult = await _majorityElectionResultBundleReader.GetElectionResultWithBundles(GuidParser.Parse(request.ElectionResultId));
        return _mapper.Map<ProtoModels.MajorityElectionResultBundles>(electionResult);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Read)]
    public override async Task<GetMajorityElectionResultBundleResponse> GetBundle(
        GetMajorityElectionResultBundleRequest request,
        ServerCallContext context)
    {
        var bundle = await _majorityElectionResultBundleReader.GetBundle(GuidParser.Parse(request.BundleId));
        return _mapper.Map<GetMajorityElectionResultBundleResponse>(bundle);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBallot.Read)]
    public override async Task<ProtoModels.MajorityElectionResultBallot> GetBallot(
        GetMajorityElectionResultBallotRequest request,
        ServerCallContext context)
    {
        var ballot = await _majorityElectionResultBundleReader.GetBallot(GuidParser.Parse(request.BundleId), request.BallotNumber);
        return _mapper.Map<ProtoModels.MajorityElectionResultBallot>(ballot);
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Create)]
    public override async Task<CreateMajorityElectionResultBundleResponse> CreateBundle(
        CreateMajorityElectionResultBundleRequest request,
        ServerCallContext context)
    {
        var response = await _majorityElectionResultBundleWriter.CreateBundle(
            GuidParser.Parse(request.ElectionResultId),
            request.BundleNumber);
        return new()
        {
            BundleId = response.Id.ToString(),
            BundleNumber = response.BundleNumber,
        };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Delete)]
    public override async Task<Empty> DeleteBundle(
        DeleteMajorityElectionResultBundleRequest request,
        ServerCallContext context)
    {
        await _majorityElectionResultBundleWriter.DeleteBundle(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBallot.Create)]
    public override async Task<CreateMajorityElectionResultBallotResponse> CreateBallot(
        CreateMajorityElectionResultBallotRequest request,
        ServerCallContext context)
    {
        var secondaryElectionResultBallots = _mapper.Map<List<SecondaryMajorityElectionResultBallot>>(request.SecondaryMajorityElectionResults);
        var ballotNumber = await _majorityElectionResultBundleWriter.CreateBallot(
            GuidParser.Parse(request.BundleId),
            request.EmptyVoteCount,
            request.IndividualVoteCount,
            request.InvalidVoteCount,
            request.SelectedCandidateIds.Select(GuidParser.Parse).ToList(),
            secondaryElectionResultBallots);
        return new CreateMajorityElectionResultBallotResponse
        {
            BundleId = request.BundleId,
            BallotNumber = ballotNumber,
        };
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBallot.Update)]
    public override async Task<Empty> UpdateBallot(
        UpdateMajorityElectionResultBallotRequest request,
        ServerCallContext context)
    {
        var secondaryElectionResultBallots = _mapper.Map<List<SecondaryMajorityElectionResultBallot>>(request.SecondaryMajorityElectionResults);
        await _majorityElectionResultBundleWriter.UpdateBallot(
            GuidParser.Parse(request.BundleId),
            request.BallotNumber,
            request.EmptyVoteCount,
            request.IndividualVoteCount,
            request.InvalidVoteCount,
            request.SelectedCandidateIds.Select(GuidParser.Parse).ToList(),
            secondaryElectionResultBallots);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBallot.Delete)]
    public override async Task<Empty> DeleteBallot(
        DeleteMajorityElectionResultBallotRequest request,
        ServerCallContext context)
    {
        await _majorityElectionResultBundleWriter.DeleteBallot(GuidParser.Parse(request.BundleId), request.BallotNumber);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.FinishSubmission)]
    public override async Task<Empty> BundleSubmissionFinished(
        MajorityElectionResultBundleSubmissionFinishedRequest request,
        ServerCallContext context)
    {
        await _majorityElectionResultBundleWriter.BundleSubmissionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.FinishSubmission)]
    public override async Task<Empty> BundleCorrectionFinished(
        MajorityElectionResultBundleCorrectionFinishedRequest request,
        ServerCallContext context)
    {
        await _majorityElectionResultBundleWriter.BundleCorrectionFinished(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Review)]
    public override async Task<Empty> RejectBundleReview(RejectMajorityElectionBundleReviewRequest request, ServerCallContext context)
    {
        await _majorityElectionResultBundleWriter.RejectBundleReview(GuidParser.Parse(request.BundleId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalBusinessResultBundle.Review)]
    public override async Task<Empty> SucceedBundleReview(SucceedMajorityElectionBundleReviewRequest request, ServerCallContext context)
    {
        await _majorityElectionResultBundleWriter.SucceedBundleReview(request.BundleIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }
}
