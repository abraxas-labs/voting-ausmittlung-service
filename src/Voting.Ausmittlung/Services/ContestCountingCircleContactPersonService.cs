// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

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
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ContestCountingCircleContactPersonService.ContestCountingCircleContactPersonServiceBase;

namespace Voting.Ausmittlung.Services;

public class ContestCountingCircleContactPersonService : ServiceBase
{
    private readonly ContestCountingCircleContactPersonWriter _writer;
    private readonly IMapper _mapper;

    public ContestCountingCircleContactPersonService(ContestCountingCircleContactPersonWriter writer, IMapper mapper)
    {
        _writer = writer;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.CountingCircleContactPerson.Create)]
    public override async Task<ProtoModels.IdValue> Create(
        CreateContestCountingCircleContactPersonRequest request,
        ServerCallContext context)
    {
        var contestId = GuidParser.Parse(request.ContestId);
        var countingCircleId = GuidParser.Parse(request.CountingCircleId);

        var id = await _writer.Create(
            contestId,
            countingCircleId,
            _mapper.Map<ContactPerson>(request.ContactPersonDuringEvent),
            request.ContactPersonSameDuringEventAsAfter,
            _mapper.Map<ContactPerson>(request.ContactPersonAfterEvent));

        return new ProtoModels.IdValue
        {
            Id = id.ToString(),
        };
    }

    [AuthorizePermission(Permissions.CountingCircleContactPerson.Update)]
    public override async Task<Empty> Update(
        UpdateContestCountingCircleContactPersonRequest request,
        ServerCallContext context)
    {
        var id = GuidParser.Parse(request.Id);
        await _writer.Update(
            id,
            _mapper.Map<ContactPerson>(request.ContactPersonDuringEvent),
            request.ContactPersonSameDuringEventAsAfter,
            _mapper.Map<ContactPerson>(request.ContactPersonAfterEvent));
        return ProtobufEmpty.Instance;
    }
}
