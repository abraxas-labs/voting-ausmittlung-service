// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using CoreModels = Voting.Ausmittlung.Core.Models;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class MajorityElectionEndResultProfile : Profile
{
    public MajorityElectionEndResultProfile()
    {
        // read models
        CreateMap<DataModels.MajorityElectionEndResultVotingCardDetail, ProtoModels.VotingCardResultDetail>();
        CreateMap<DataModels.MajorityElectionEndResultCountOfVotersInformationSubTotal, ProtoModels.CountOfVotersInformationSubTotal>();
        CreateMap<DataModels.MajorityElectionEndResult, ProtoModels.CountOfVotersInformation>()
            .ForMember(dst => dst.SubTotalInfo, opts => opts.MapFrom(src => src.CountOfVotersInformationSubTotals));
        CreateMap<DataModels.MajorityElectionEndResult, ProtoModels.MajorityElectionEndResult>()
            .ForMember(dst => dst.Contest, opts => opts.MapFrom(src => src.MajorityElection.Contest))
            .ForMember(dst => dst.CountOfVotersInformation, opts => opts.MapFrom(src => src));
        CreateMap<DataModels.SecondaryMajorityElectionEndResult, ProtoModels.SecondaryMajorityElectionEndResult>();
        CreateMap<DataModels.MajorityElectionCandidateEndResult, ProtoModels.MajorityElectionCandidateEndResult>();
        CreateMap<DataModels.SecondaryMajorityElectionCandidateEndResult, ProtoModels.MajorityElectionCandidateEndResult>();

        CreateMap<CoreModels.MajorityElectionEndResultAvailableLotDecisions, ProtoModels.MajorityElectionEndResultAvailableLotDecisions>();
        CreateMap<CoreModels.SecondaryMajorityElectionEndResultAvailableLotDecisions, ProtoModels.SecondaryMajorityElectionEndResultAvailableLotDecisions>();
        CreateMap<CoreModels.MajorityElectionEndResultAvailableLotDecision, ProtoModels.MajorityElectionEndResultAvailableLotDecision>();

        // write
        CreateMap<UpdateMajorityElectionEndResultLotDecisionRequest, ElectionEndResultLotDecision>();
    }
}
