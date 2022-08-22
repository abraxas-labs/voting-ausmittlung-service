// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class ElectionGroupProfile : Profile
{
    public ElectionGroupProfile()
    {
        CreateMap<ElectionGroupEventData, ElectionGroup>()
            .ForMember(dst => dst.PrimaryMajorityElectionId, opts => opts.MapFrom(src => src.PrimaryMajorityElectionId));
    }
}
