// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ContestCountingCircleDetailsProfile : Profile
{
    public ContestCountingCircleDetailsProfile()
    {
        // read
        CreateMap<DataModels.ContestCountingCircleDetails, ProtoModels.ContestCountingCircleDetails>()
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(src => src.CountingCircle.BasisCountingCircleId))
            .ForMember(dst => dst.CountOfVotersInformation, opts => opts.MapFrom(src => src));
        CreateMap<DataModels.ContestCountingCircleDetails, ProtoModels.CountOfVotersInformation>()
            .ForMember(dst => dst.SubTotalInfo, opts => opts.MapFrom(src => src.CountOfVotersInformationSubTotals));

        CreateMap<DataModels.ContestDetails, ProtoModels.CountOfVotersInformation>()
            .ForMember(dst => dst.SubTotalInfo, opts => opts.MapFrom(src => src.CountOfVotersInformationSubTotals));

        CreateMap<DataModels.ContestDomainOfInfluenceDetails, ProtoModels.CountOfVotersInformation>()
            .ForMember(dst => dst.SubTotalInfo, opts => opts.MapFrom(src => src.CountOfVotersInformationSubTotals));

        CreateMap<DataModels.AggregatedVotingCardResultDetail, ProtoModels.VotingCardResultDetail>();
        CreateMap<DataModels.AggregatedCountOfVotersInformationSubTotal, ProtoModels.CountOfVotersInformationSubTotal>();

        // write
        CreateMap<UpdateContestCountingCircleDetailsRequest, ContestCountingCircleDetails>()
            .ForMember(dst => dst.CountOfVotersInformation, opts => opts.MapFrom(src => src.CountOfVoters));
        CreateMap<IEnumerable<UpdateCountOfVotersInformationSubTotalRequest>, CountOfVotersInformation>()
            .ForMember(dst => dst.SubTotalInfo, opts => opts.MapFrom(src => src))
            .ForMember(dst => dst.TotalCountOfVoters, opts => opts.Ignore());
        CreateMap<UpdateVotingCardResultDetailRequest, VotingCardResultDetail>();
        CreateMap<UpdateCountOfVotersInformationSubTotalRequest, CountOfVotersInformationSubTotal>();
    }
}
