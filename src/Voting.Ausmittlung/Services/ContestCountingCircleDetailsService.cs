// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceBase;

namespace Voting.Ausmittlung.Services;

public class ContestCountingCircleDetailsService : ServiceBase
{
    private readonly ContestCountingCircleDetailsWriter _writer;
    private readonly IMapper _mapper;
    private readonly ContestCountingCircleDetailsValidationSummaryBuilder _ccDetailsValidationSummaryBuilder;

    public ContestCountingCircleDetailsService(
        ContestCountingCircleDetailsWriter writer,
        IMapper mapper,
        ContestCountingCircleDetailsValidationSummaryBuilder ccDetailsValidationSummaryBuilder)
    {
        _writer = writer;
        _mapper = mapper;
        _ccDetailsValidationSummaryBuilder = ccDetailsValidationSummaryBuilder;
    }

    [AuthorizePermission(Permissions.ContestCountingCircleDetails.Update)]
    public override async Task<Empty> UpdateDetails(
        UpdateContestCountingCircleDetailsRequest request,
        ServerCallContext context)
    {
        var ccDetails = _mapper.Map<ContestCountingCircleDetails>(request);
        await _writer.CreateOrUpdate(ccDetails);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ContestCountingCircleDetails.Update)]
    public override async Task<ProtoModels.ValidationSummary> ValidateUpdateDetails(
        ValidateUpdateContestCountingCircleDetailsRequest request,
        ServerCallContext context)
    {
        var ccDetails = _mapper.Map<ContestCountingCircleDetails>(request.Request);
        var summary = await _ccDetailsValidationSummaryBuilder.BuildUpdateContestCountingCircleDetailsValidationSummary(ccDetails);
        return _mapper.Map<ProtoModels.ValidationSummary>(summary);
    }
}
