// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class MajorityElectionResultProfile : Profile
{
    public MajorityElectionResultProfile()
    {
        CreateMap<MajorityElectionResultEntryParamsEventData, MajorityElectionResultEntryParams>();

        CreateMap<MajorityElectionWriteInBallotImported, MajorityElectionWriteInBallot>()
            .ForMember(dst => dst.WriteInPositions, opts => opts.MapFrom(src => src.WriteInMappingIds));
        CreateMap<string, MajorityElectionWriteInBallotPosition>(MemberList.None)
            .ForMember(dst => dst.WriteInMappingId, opts => opts.MapFrom(src => src));

        CreateMap<SecondaryMajorityElectionWriteInBallotImported, SecondaryMajorityElectionWriteInBallot>()
            .ForMember(dst => dst.WriteInPositions, opts => opts.MapFrom(src => src.WriteInMappingIds));
        CreateMap<string, SecondaryMajorityElectionWriteInBallotPosition>(MemberList.None)
            .ForMember(dst => dst.WriteInMappingId, opts => opts.MapFrom(src => src));
    }
}
