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
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.MajorityElectionService.MajorityElectionServiceBase;

namespace Voting.Ausmittlung.Services;

public class MajorityElectionService : ServiceBase
{
    private readonly MajorityElectionReader _majorityElectionReader;
    private readonly IMapper _mapper;

    public MajorityElectionService(MajorityElectionReader majorityElectionReader, IMapper mapper)
    {
        _majorityElectionReader = majorityElectionReader;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.MajorityElectionCandidate.Read)]
    public override async Task<ProtoModels.MajorityElectionCandidates> ListCandidates(
        ListMajorityElectionCandidatesRequest request,
        ServerCallContext context)
    {
        var election = await _majorityElectionReader.GetWithCandidates(
            GuidParser.Parse(request.ElectionId),
            request.IncludeCandidatesOfSecondaryElection);
        return _mapper.Map<ProtoModels.MajorityElectionCandidates>(election);
    }

    [AuthorizePermission(Permissions.MajorityElectionCandidate.Read)]
    public override async Task<ProtoModels.SecondaryMajorityElectionCandidates> ListSecondaryElectionCandidates(
        ListSecondaryMajorityElectionCandidatesRequest request,
        ServerCallContext context)
    {
        var election = await _majorityElectionReader.GetSecondaryWithCandidates(GuidParser.Parse(request.SecondaryElectionId));
        return _mapper.Map<ProtoModels.SecondaryMajorityElectionCandidates>(election);
    }
}
