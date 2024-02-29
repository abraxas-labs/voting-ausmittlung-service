// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ContestCountingCircleElectorateService.ContestCountingCircleElectorateServiceBase;

namespace Voting.Ausmittlung.Services;

public class ContestCountingCircleElectorateService : ServiceBase
{
    private readonly IMapper _mapper;
    private readonly ContestCountingCircleElectorateWriter _writer;

    public ContestCountingCircleElectorateService(ContestCountingCircleElectorateWriter writer, IMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.ContestCountingCircleElectorate.Update)]
    public override async Task<Empty> UpdateElectorates(
        UpdateContestCountingCircleElectoratesRequest request,
        ServerCallContext context)
    {
        await _writer.UpdateElectorates(
            GuidParser.Parse(request.ContestId),
            GuidParser.Parse(request.CountingCircleId),
            _mapper.Map<List<ContestCountingCircleElectorate>>(request.Electorates));

        return ProtobufEmpty.Instance;
    }
}
