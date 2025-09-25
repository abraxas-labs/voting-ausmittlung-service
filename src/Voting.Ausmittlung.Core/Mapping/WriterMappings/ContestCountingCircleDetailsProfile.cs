// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using DataModels = Voting.Ausmittlung.Data.Models;
using EventsV2 = Abraxas.Voting.Ausmittlung.Events.V2;

namespace Voting.Ausmittlung.Core.Mapping.WriterMappings;

public class ContestCountingCircleDetailsProfile : Profile
{
    public ContestCountingCircleDetailsProfile()
    {
        CreateMap<ContestCountingCircleDetailsCreated, ContestCountingCircleDetailsAggregate>();
        CreateMap<ContestCountingCircleDetailsUpdated, ContestCountingCircleDetailsAggregate>();
        CreateMap<EventsV2.ContestCountingCircleDetailsCreated, ContestCountingCircleDetailsAggregate>();
        CreateMap<EventsV2.ContestCountingCircleDetailsUpdated, ContestCountingCircleDetailsAggregate>();

        CreateMap<CountOfVotersInformation, CountOfVotersInformationEventData>().ReverseMap();
        CreateMap<CountOfVotersInformationSubTotal, CountOfVotersInformationSubTotalEventData>().ReverseMap();
        CreateMap<CountOfVotersInformationSubTotal, EventsV2.Data.CountOfVotersInformationSubTotalEventData>().ReverseMap();
        CreateMap<VotingCardResultDetail, VotingCardResultDetailEventData>().ReverseMap();
        CreateMap<ContestCountingCircleDetails, DataModels.ContestCountingCircleDetails>();
        CreateMap<CountOfVotersInformationSubTotal, DataModels.CountOfVotersInformationSubTotal>();
    }
}
