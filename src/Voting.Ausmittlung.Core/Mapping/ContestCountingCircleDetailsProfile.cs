// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class ContestCountingCircleDetailsProfile : Profile
{
    public ContestCountingCircleDetailsProfile()
    {
        CreateMap<ContestCountingCircleDetailsCreated, ContestCountingCircleDetails>()
            .ForMember(dst => dst.TotalCountOfVoters, opts => opts.MapFrom(src => src.CountOfVotersInformation.TotalCountOfVoters))
            .ForMember(dst => dst.CountOfVotersInformationSubTotals, opts => opts.MapFrom(src => src.CountOfVotersInformation.SubTotalInfo));
        CreateMap<ContestCountingCircleDetailsUpdated, ContestCountingCircleDetails>()
            .ForMember(dst => dst.TotalCountOfVoters, opts => opts.MapFrom(src => src.CountOfVotersInformation.TotalCountOfVoters))
            .ForMember(dst => dst.CountOfVotersInformationSubTotals, opts => opts.MapFrom(src => src.CountOfVotersInformation.SubTotalInfo));

        CreateMap<CountOfVotersInformationSubTotalEventData, CountOfVotersInformationSubTotal>();
        CreateMap<VotingCardResultDetailEventData, VotingCardResultDetail>();
    }
}
