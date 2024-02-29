// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class DomainOfInfluenceProfile : Profile
{
    public DomainOfInfluenceProfile()
    {
        // read
        CreateMap<DataModels.DomainOfInfluence, ProtoModels.DomainOfInfluence>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.BasisDomainOfInfluenceId))
            .ForMember(dst => dst.ParentId, opts => opts.MapFrom(src => src.Parent != null ? src.Parent.BasisDomainOfInfluenceId.ToString() : string.Empty))
            .ForMember(dst => dst.Children, opts => opts.Ignore());
        CreateMap<DataModels.DomainOfInfluenceCantonDefaults, ProtoModels.DomainOfInfluenceCantonDefaults>();
    }
}
