// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using CoreModels = Voting.Ausmittlung.Core.Models;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public sealed class ContestProfile : Profile
{
    public ContestProfile()
    {
        // read
        CreateMap<DataModels.Contest, ProtoModels.ContestSummary>();
        CreateMap<DataModels.Contest, ProtoModels.Contest>()
            .ForMember(dst => dst.DomainOfInfluenceId, opts => opts.MapFrom(src => src.DomainOfInfluence.BasisDomainOfInfluenceId));
        CreateMap<CoreModels.ContestSummary, ProtoModels.ContestSummary>()
            .IncludeMembers(src => src.Contest)
            .ForMember(dst => dst.DomainOfInfluenceId, opts => opts.MapFrom(src => src.Contest.DomainOfInfluence.BasisDomainOfInfluenceId));
        CreateMap<IEnumerable<CoreModels.ContestSummary>, ProtoModels.ContestSummaries>()
            .ForMember(dst => dst.ContestSummaries_, opts => opts.MapFrom(src => src));
        CreateMap<CoreModels.ContestSummaryEntryDetails, ProtoModels.ContestSummaryEntryDetails>();
        CreateMap<DataModels.ContestCantonDefaults, ProtoModels.ContestCantonDefaults>();
        CreateMap<DataModels.ContestCantonDefaultsCountingCircleResultStateDescription, ProtoModels.CountingCircleResultStateDescription>();
    }
}
