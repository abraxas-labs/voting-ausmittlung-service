// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;

namespace Voting.Ausmittlung.Core.Mapping;

public class DoubleProportionalResultProfile : Profile
{
    public DoubleProportionalResultProfile()
    {
        CreateMap<Models.DoubleProportionalResultSuperApportionmentLotDecision, Domain.DoubleProportionalResultSuperApportionmentLotDecision>();
        CreateMap<Models.DoubleProportionalResultSuperApportionmentLotDecisionColumn, Domain.DoubleProportionalResultSuperApportionmentLotDecisionColumn>()
            .ForMember(dst => dst.ListId, opts => opts.MapFrom(src => src.List != null ? (Guid?)src.List.Id : null))
            .ForMember(dst => dst.UnionListId, opts => opts.MapFrom(src => src.UnionList != null ? (Guid?)src.UnionList.Id : null));

        CreateMap<Models.DoubleProportionalResultSubApportionmentLotDecision, Domain.DoubleProportionalResultSubApportionmentLotDecision>();
        CreateMap<Models.DoubleProportionalResultSubApportionmentLotDecisionColumn, Domain.DoubleProportionalResultSubApportionmentLotDecisionColumn>();
        CreateMap<Models.DoubleProportionalResultSubApportionmentLotDecisionCell, Domain.DoubleProportionalResultSubApportionmentLotDecisionCell>();
    }
}
