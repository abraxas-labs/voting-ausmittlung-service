// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Mapping;

public class CountOfVotersProfile : Profile
{
    public CountOfVotersProfile()
    {
        CreateMap<PoliticalBusinessCountOfVotersEventData, PoliticalBusinessNullableCountOfVoters>().ReverseMap();
        CreateMap<PoliticalBusinessCountOfVotersEventData, PoliticalBusinessCountOfVoters>().ReverseMap();
        CreateMap<DomainModels.VotingCardResultDetail, VotingCardResultDetail>();
        CreateMap<DomainModels.PoliticalBusinessCountOfVoters, PoliticalBusinessNullableCountOfVoters>();
    }
}
