// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping.WriterMappings;

public class ContestCountingCircleDetailsProfile : Profile
{
    public ContestCountingCircleDetailsProfile()
    {
        CreateMap<ContestCountingCircleDetailsCreated, ContestCountingCircleDetailsAggregate>();
        CreateMap<ContestCountingCircleDetailsUpdated, ContestCountingCircleDetailsAggregate>();

        CreateMap<CountOfVotersInformation, CountOfVotersInformationEventData>().ReverseMap();
        CreateMap<CountOfVotersInformationSubTotal, CountOfVotersInformationSubTotalEventData>().ReverseMap();
        CreateMap<VotingCardResultDetail, VotingCardResultDetailEventData>().ReverseMap();
        CreateMap<ContestCountingCircleDetails, DataModels.ContestCountingCircleDetails>()
            .ForMember(dst => dst.TotalCountOfVoters, opts => opts.MapFrom(src => src.CountOfVotersInformation.TotalCountOfVoters))
            .ForMember(dst => dst.CountOfVotersInformationSubTotals, opts => opts.MapFrom(src => src.CountOfVotersInformation.SubTotalInfo));
        CreateMap<CountOfVotersInformationSubTotal, DataModels.CountOfVotersInformationSubTotal>();
    }
}
