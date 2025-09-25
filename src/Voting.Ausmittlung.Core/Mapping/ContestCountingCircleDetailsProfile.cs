// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using EventsV2 = Abraxas.Voting.Ausmittlung.Events.V2;

namespace Voting.Ausmittlung.Core.Mapping;

public class ContestCountingCircleDetailsProfile : Profile
{
    public ContestCountingCircleDetailsProfile()
    {
        CreateMap<ContestCountingCircleDetailsCreated, ContestCountingCircleDetails>()
            .ForMember(dst => dst.CountOfVotersInformationSubTotals, opts => opts.MapFrom(src => src.CountOfVotersInformation.SubTotalInfo));
        CreateMap<ContestCountingCircleDetailsUpdated, ContestCountingCircleDetails>()
            .ForMember(dst => dst.CountOfVotersInformationSubTotals, opts => opts.MapFrom(src => src.CountOfVotersInformation.SubTotalInfo));
        CreateMap<EventsV2.ContestCountingCircleDetailsCreated, ContestCountingCircleDetails>();
        CreateMap<EventsV2.ContestCountingCircleDetailsUpdated, ContestCountingCircleDetails>();

        CreateMap<CountOfVotersInformationSubTotalEventData, CountOfVotersInformationSubTotal>();
        CreateMap<VotingCardResultDetailEventData, VotingCardResultDetail>();
        CreateMap<EventsV2.Data.CountOfVotersInformationSubTotalEventData, CountOfVotersInformationSubTotal>();
    }
}
