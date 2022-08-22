// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Lib.Grpc;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceBase;

namespace Voting.Ausmittlung.Services;

[Authorize]
public class ContestCountingCircleDetailsService : ServiceBase
{
    private readonly ContestCountingCircleDetailsWriter _writer;
    private readonly IMapper _mapper;
    private readonly ContestCountingCircleDetailsValidationResultsBuilder _ccDetailsValidationResultsBuilder;

    public ContestCountingCircleDetailsService(
        ContestCountingCircleDetailsWriter writer,
        IMapper mapper,
        ContestCountingCircleDetailsValidationResultsBuilder ccDetailsValidationResultsBuilder)
    {
        _writer = writer;
        _mapper = mapper;
        _ccDetailsValidationResultsBuilder = ccDetailsValidationResultsBuilder;
    }

    public override async Task<Empty> UpdateDetails(
        UpdateContestCountingCircleDetailsRequest request,
        ServerCallContext context)
    {
        var ccDetails = _mapper.Map<ContestCountingCircleDetails>(request);
        await _writer.CreateOrUpdate(ccDetails);
        return ProtobufEmpty.Instance;
    }

    public override async Task<ProtoModels.ValidationOverview> ValidateUpdateDetails(
        ValidateUpdateContestCountingCircleDetailsRequest request,
        ServerCallContext context)
    {
        var ccDetails = _mapper.Map<ContestCountingCircleDetails>(request.Request);
        var results = await _ccDetailsValidationResultsBuilder.BuildUpdateContestCountingCircleDetailsValidationResults(ccDetails);
        return _mapper.Map<ProtoModels.ValidationOverview>(results);
    }
}
