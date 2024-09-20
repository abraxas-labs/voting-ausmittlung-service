// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class CountingCircleProfile : Profile
{
    public CountingCircleProfile()
    {
        // read
        CreateMap<DataModels.CountingCircle, ProtoModels.CountingCircle>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.BasisCountingCircleId));
        CreateMap<DataModels.Authority, ProtoModels.Authority>();
        CreateMap<DataModels.CountingCircleContactPerson, ProtoModels.ContactPerson>();
        CreateMap<IEnumerable<DataModels.CountingCircle>, ProtoModels.CountingCircles>()
            .ForMember(dst => dst.CountingCircles_, opts => opts.MapFrom(x => x));
    }
}
