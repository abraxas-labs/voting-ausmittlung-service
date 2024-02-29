// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Lib.Common;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.DomainOfInfluenceService.DomainOfInfluenceServiceBase;

namespace Voting.Ausmittlung.Services;

public class DomainOfInfluenceService : ServiceBase
{
    private readonly ContestReader _contestReader;
    private readonly SimplePoliticalBusinessReader _simplePoliticalBusinessReader;
    private readonly SimpleCountingCircleResultReader _simpleCountingCircleResultReader;
    private readonly IMapper _mapper;

    public DomainOfInfluenceService(
        ContestReader contestReader,
        SimplePoliticalBusinessReader simplePoliticalBusinessReader,
        SimpleCountingCircleResultReader simpleCountingCircleResultReader,
        IMapper mapper)
    {
        _contestReader = contestReader;
        _simplePoliticalBusinessReader = simplePoliticalBusinessReader;
        _simpleCountingCircleResultReader = simpleCountingCircleResultReader;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.CantonDefaults.Read)]
    public override async Task<ProtoModels.DomainOfInfluenceCantonDefaults> GetCantonDefaults(
        GetCantonDefaultsRequest request, ServerCallContext context)
    {
        var cantonDefaults = request.IdCase switch
        {
            GetCantonDefaultsRequest.IdOneofCase.ContestId => await _contestReader.GetCantonDefaults(GuidParser.Parse(request.ContestId)),
            GetCantonDefaultsRequest.IdOneofCase.PoliticalBusinessId => await _simplePoliticalBusinessReader.GetCantonDefaults(GuidParser.Parse(request.PoliticalBusinessId)),
            GetCantonDefaultsRequest.IdOneofCase.CountingCircleResultId => await _simpleCountingCircleResultReader.GetCantonDefaults(GuidParser.Parse(request.CountingCircleResultId)),
            _ => throw new ValidationException($"either {nameof(GetCantonDefaultsRequest.IdOneofCase.ContestId)} or {nameof(GetCantonDefaultsRequest.IdOneofCase.PoliticalBusinessId)} or {nameof(GetCantonDefaultsRequest.IdOneofCase.CountingCircleResultId)} must be set"),
        };
        return _mapper.Map<ProtoModels.DomainOfInfluenceCantonDefaults>(cantonDefaults);
    }
}
