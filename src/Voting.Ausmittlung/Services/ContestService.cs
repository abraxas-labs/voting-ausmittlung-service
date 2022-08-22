// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ContestService.ContestServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class ContestService : ServiceBase
{
    private readonly ContestReader _contestReader;
    private readonly IMapper _mapper;

    public ContestService(ContestReader contestReader, IMapper mapper)
    {
        _contestReader = contestReader;
        _mapper = mapper;
    }

    public override async Task<ProtoModels.ContestSummaries> ListSummaries(
        ListContestSummariesRequest request, ServerCallContext context)
    {
        var states = request.States.Cast<ContestState>().ToList();
        var summaries = await _contestReader.ListSummaries(states);
        return _mapper.Map<ProtoModels.ContestSummaries>(summaries);
    }

    public override async Task<ProtoModels.CountingCircles> GetAccessibleCountingCircles(
        GetAccessibleCountingCirclesRequest request,
        ServerCallContext context)
    {
        var countingCircles = await _contestReader.GetAccessibleCountingCircles(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.CountingCircles>(countingCircles);
    }

    public override async Task<ProtoModels.Contest> Get(GetContestRequest request, ServerCallContext context)
    {
        var contest = await _contestReader.Get(GuidParser.Parse(request.Id));
        return _mapper.Map<ProtoModels.Contest>(contest);
    }

    public override async Task<ProtoModels.PoliticalBusinessUnions> ListPoliticalBusinessUnions(ListPoliticalBusinessUnionsRequest request, ServerCallContext context)
    {
        var unions = await _contestReader.ListPoliticalBusinessUnions(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ProtoModels.PoliticalBusinessUnions>(unions);
    }
}
