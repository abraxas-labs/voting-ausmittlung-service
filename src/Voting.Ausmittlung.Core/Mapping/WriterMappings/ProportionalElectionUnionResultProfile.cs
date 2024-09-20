// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Mapping.WriterMappings;

public class ProportionalElectionUnionResultProfile : Profile
{
    public ProportionalElectionUnionResultProfile()
    {
        CreateMap<ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated, DoubleProportionalResultSuperApportionmentLotDecision>();
        CreateMap<ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated, DoubleProportionalResultSubApportionmentLotDecision>();

        CreateMap<DoubleProportionalResultSuperApportionmentLotDecisionColumn, ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionColumnEventData>()
            .ForMember(dst => dst.UnionListId, opts => opts.MapFrom(src => src.UnionListId!.Value))
            .ReverseMap();
        CreateMap<DoubleProportionalResultSubApportionmentLotDecisionColumn, ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionColumnEventData>()
            .ReverseMap();
        CreateMap<DoubleProportionalResultSubApportionmentLotDecisionCell, ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData>()
            .ReverseMap();
    }
}
