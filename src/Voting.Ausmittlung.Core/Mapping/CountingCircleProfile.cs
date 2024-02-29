// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public sealed class CountingCircleProfile : Profile
{
    public CountingCircleProfile()
    {
        CreateMap<CountingCircleEventData, CountingCircle>()
            .ForMember(dst => dst.BasisCountingCircleId, opts => opts.MapFrom(src => src.Id));
        CreateMap<AuthorityEventData, Authority>();
        CreateMap<ContactPersonEventData, CountingCircleContactPerson>();
        CreateMap<CountingCircleElectorateEventData, CountingCircleElectorate>();
    }
}
