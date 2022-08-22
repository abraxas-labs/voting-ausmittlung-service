// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Lib.Common;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.MajorityElectionService.MajorityElectionServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class MajorityElectionService : ServiceBase
{
    private readonly MajorityElectionReader _majorityElectionReader;
    private readonly IMapper _mapper;

    public MajorityElectionService(MajorityElectionReader majorityElectionReader, IMapper mapper)
    {
        _majorityElectionReader = majorityElectionReader;
        _mapper = mapper;
    }

    public override async Task<ProtoModels.MajorityElectionCandidates> ListCandidates(
        ListMajorityElectionCandidatesRequest request,
        ServerCallContext context)
    {
        var election = await _majorityElectionReader.GetWithCandidates(
            GuidParser.Parse(request.ElectionId),
            request.IncludeCandidatesOfSecondaryElection);
        return _mapper.Map<ProtoModels.MajorityElectionCandidates>(election);
    }

    public override async Task<ProtoModels.SecondaryMajorityElectionCandidates> ListSecondaryElectionCandidates(
        ListSecondaryMajorityElectionCandidatesRequest request,
        ServerCallContext context)
    {
        var election = await _majorityElectionReader.GetSecondaryWithCandidates(GuidParser.Parse(request.SecondaryElectionId));
        return _mapper.Map<ProtoModels.SecondaryMajorityElectionCandidates>(election);
    }
}
