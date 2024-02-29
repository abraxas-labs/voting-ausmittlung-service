// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using AutoMapper.Extensions.EnumMapping;
using Voting.Ausmittlung.Data.Models;
using BasisSharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Core.Mapping;

public class EnumProfile : Profile
{
    public EnumProfile()
    {
        // explicitly map deprecated values to the corresponding new value.
        CreateMap<BasisSharedProto.ProportionalElectionMandateAlgorithm, ProportionalElectionMandateAlgorithm>()
            .ConvertUsingEnumMapping(opt => opt
                .MapByName()
                .MapValue(BasisSharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum, ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum)
                .MapValue(BasisSharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum, ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum))
            .ReverseMap();
    }
}
