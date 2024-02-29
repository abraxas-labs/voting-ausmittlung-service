// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Lib.Common;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ProportionalElectionService.ProportionalElectionServiceBase;

namespace Voting.Ausmittlung.Services;

public class ProportionalElectionService : ServiceBase
{
    private readonly ProportionalElectionReader _proportionalElectionReader;
    private readonly IMapper _mapper;

    public ProportionalElectionService(ProportionalElectionReader proportionalElectionReader, IMapper mapper)
    {
        _proportionalElectionReader = proportionalElectionReader;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.ProportionalElectionList.Read)]
    public override async Task<ProtoModels.ProportionalElectionLists> GetLists(
        GetProportionalElectionListsRequest request,
        ServerCallContext context)
    {
        var result = await _proportionalElectionReader.GetLists(GuidParser.Parse(request.ElectionId));
        return _mapper.Map<ProtoModels.ProportionalElectionLists>(result);
    }

    [AuthorizePermission(Permissions.ProportionalElectionList.Read)]
    public override async Task<ProtoModels.ProportionalElectionList> GetList(
        GetProportionalElectionListRequest request,
        ServerCallContext context)
    {
        var list = await _proportionalElectionReader.GetList(GuidParser.Parse(request.ListId));
        return _mapper.Map<ProtoModels.ProportionalElectionList>(list);
    }

    [AuthorizePermission(Permissions.ProportionalElectionCandidate.Read)]
    public override async Task<ProtoModels.ProportionalElectionCandidates> ListCandidates(
        ListProportionalElectionCandidatesRequest request,
        ServerCallContext context)
    {
        var candidates = await _proportionalElectionReader.ListCandidates(GuidParser.Parse(request.ElectionId));
        return _mapper.Map<ProtoModels.ProportionalElectionCandidates>(candidates);
    }
}
