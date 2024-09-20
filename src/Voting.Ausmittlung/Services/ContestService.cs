// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ContestService.ContestServiceBase;

namespace Voting.Ausmittlung.Services;

public class ContestService : ServiceBase
{
    private readonly ContestReader _contestReader;
    private readonly SimplePoliticalBusinessReader _simplePoliticalBusinessReader;
    private readonly SimpleCountingCircleResultReader _simpleCountingCircleResultReader;
    private readonly IMapper _mapper;

    public ContestService(ContestReader contestReader, SimplePoliticalBusinessReader simplePoliticalBusinessReader, SimpleCountingCircleResultReader simpleCountingCircleResultReader, IMapper mapper)
    {
        _contestReader = contestReader;
        _simplePoliticalBusinessReader = simplePoliticalBusinessReader;
        _simpleCountingCircleResultReader = simpleCountingCircleResultReader;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.Contest.Read)]
    public override async Task<ProtoModels.ContestSummaries> ListSummaries(
        ListContestSummariesRequest request, ServerCallContext context)
    {
        var states = request.States.Cast<ContestState>().ToList();
        var summaries = await _contestReader.ListSummaries(states);
        return _mapper.Map<ProtoModels.ContestSummaries>(summaries);
    }

    [AuthorizePermission(Permissions.Contest.Read)]
    public override async Task<ProtoModels.CountingCircles> GetAccessibleCountingCircles(
        GetAccessibleCountingCirclesRequest request,
        ServerCallContext context)
    {
        var countingCircles = await _contestReader.GetAccessibleCountingCircles(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.CountingCircles>(countingCircles);
    }

    [AuthorizePermission(Permissions.Contest.Read)]
    public override async Task<ProtoModels.Contest> Get(GetContestRequest request, ServerCallContext context)
    {
        var contest = await _contestReader.Get(GuidParser.Parse(request.Id));
        return _mapper.Map<ProtoModels.Contest>(contest);
    }

    [AuthorizePermission(Permissions.Contest.Read)]
    public override async Task<ProtoModels.ContestCantonDefaults> GetCantonDefaults(
        GetCantonDefaultsRequest request, ServerCallContext context)
    {
        var cantonDefaults = request.IdCase switch
        {
            GetCantonDefaultsRequest.IdOneofCase.ContestId => await _contestReader.GetCantonDefaults(GuidParser.Parse(request.ContestId)),
            GetCantonDefaultsRequest.IdOneofCase.PoliticalBusinessId => await _simplePoliticalBusinessReader.GetCantonDefaults(GuidParser.Parse(request.PoliticalBusinessId)),
            GetCantonDefaultsRequest.IdOneofCase.CountingCircleResultId => await _simpleCountingCircleResultReader.GetCantonDefaults(GuidParser.Parse(request.CountingCircleResultId)),
            _ => throw new ValidationException($"either {nameof(GetCantonDefaultsRequest.IdOneofCase.ContestId)} or {nameof(GetCantonDefaultsRequest.IdOneofCase.PoliticalBusinessId)} or {nameof(GetCantonDefaultsRequest.IdOneofCase.CountingCircleResultId)} must be set"),
        };
        return _mapper.Map<ProtoModels.ContestCantonDefaults>(cantonDefaults);
    }
}
