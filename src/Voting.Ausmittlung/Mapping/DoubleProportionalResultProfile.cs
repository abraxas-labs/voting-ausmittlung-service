// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using CoreModels = Voting.Ausmittlung.Core.Models;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class DoubleProportionalResultProfile : Profile
{
    public DoubleProportionalResultProfile()
    {
        // read
        CreateMap<DataModels.DoubleProportionalResult, ProtoModels.DoubleProportionalResult>()
            .ForMember(
                dst => dst.Contest,
                opts => opts.MapFrom(src => src.ProportionalElectionUnion != null ? src.ProportionalElectionUnion.Contest : src.ProportionalElection!.Contest));

        CreateMap<DataModels.DoubleProportionalResultColumn, ProtoModels.DoubleProportionalResultColumn>();
        CreateMap<DataModels.DoubleProportionalResultRow, ProtoModels.DoubleProportionalResultRow>();
        CreateMap<DataModels.DoubleProportionalResultCell, ProtoModels.DoubleProportionalResultCell>();

        CreateMap<IEnumerable<CoreModels.DoubleProportionalResultSuperApportionmentLotDecision>, ProtoModels.DoubleProportionalResultSuperApportionmentAvailableLotDecisions>()
            .ForMember(dst => dst.LotDecisions, opts => opts.MapFrom(src => src));
        CreateMap<CoreModels.DoubleProportionalResultSuperApportionmentLotDecision, ProtoModels.DoubleProportionalResultSuperApportionmentLotDecision>();
        CreateMap<CoreModels.DoubleProportionalResultSuperApportionmentLotDecisionColumn, ProtoModels.DoubleProportionalResultSuperApportionmentLotDecisionColumn>();

        CreateMap<IEnumerable<CoreModels.DoubleProportionalResultSubApportionmentLotDecision>, ProtoModels.DoubleProportionalResultSubApportionmentAvailableLotDecisions>()
            .ForMember(dst => dst.LotDecisions, opts => opts.MapFrom(src => src));
        CreateMap<CoreModels.DoubleProportionalResultSubApportionmentLotDecision, ProtoModels.DoubleProportionalResultSubApportionmentLotDecision>();
        CreateMap<CoreModels.DoubleProportionalResultSubApportionmentLotDecisionColumn, ProtoModels.DoubleProportionalResultSubApportionmentLotDecisionColumn>();
        CreateMap<CoreModels.DoubleProportionalResultSubApportionmentLotDecisionCell, ProtoModels.DoubleProportionalResultSubApportionmentLotDecisionCell>();
    }
}
