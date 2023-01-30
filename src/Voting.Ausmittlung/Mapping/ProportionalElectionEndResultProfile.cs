// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using CoreModels = Voting.Ausmittlung.Core.Models;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ProportionalElectionEndResultProfile : Profile
{
    public ProportionalElectionEndResultProfile()
    {
        // read
        CreateMap<DataModels.ProportionalElectionEndResult, ProtoModels.ProportionalElectionEndResult>()
            .ForMember(dst => dst.Contest, opts => opts.MapFrom(src => src.ProportionalElection.Contest))
            .ForMember(dst => dst.DomainOfInfluenceDetails, opts => opts.MapFrom(src => src.ProportionalElection.DomainOfInfluence.Details));
        CreateMap<DataModels.ProportionalElectionListEndResult, ProtoModels.ProportionalElectionListEndResult>()
            .ForMember(dst => dst.ListUnion, opts => opts.MapFrom(src => src.List.ProportionalElectionListUnion))
            .ForMember(dst => dst.SubListUnion, opts => opts.MapFrom(src => src.List.ProportionalElectionSubListUnion));
        CreateMap<DataModels.ProportionalElectionCandidateEndResult, ProtoModels.ProportionalElectionCandidateEndResult>();

        CreateMap<CoreModels.ProportionalElectionListEndResultAvailableLotDecisions, ProtoModels.ProportionalElectionListEndResultAvailableLotDecisions>();
        CreateMap<CoreModels.ProportionalElectionEndResultAvailableLotDecision, ProtoModels.ProportionalElectionEndResultAvailableLotDecision>();

        // write
        CreateMap<UpdateProportionalElectionEndResultLotDecisionRequest, ElectionEndResultLotDecision>();
        CreateMap<EnterProportionalElectionManualCandidateEndResultRequest, ProportionalElectionManualCandidateEndResult>();
    }
}
