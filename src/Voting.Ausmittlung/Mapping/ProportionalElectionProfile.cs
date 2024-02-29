// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ProportionalElectionProfile : Profile
{
    public ProportionalElectionProfile()
    {
        // read
        CreateMap<DataModels.ProportionalElection, ProtoModels.ProportionalElection>()
            .ForMember(dst => dst.DomainOfInfluenceId, opts => opts.MapFrom(src => src.DomainOfInfluence.BasisDomainOfInfluenceId));
        CreateMap<DataModels.ProportionalElectionList, ProtoModels.ProportionalElectionList>();
        CreateMap<IEnumerable<DataModels.ProportionalElectionList>, ProtoModels.ProportionalElectionLists>()
            .ForMember(dst => dst.Lists, opts => opts.MapFrom(src => src));
        CreateMap<DataModels.ProportionalElectionCandidate, ProtoModels.ProportionalElectionCandidate>()
            .ForMember(dst => dst.ListId, opts => opts.MapFrom(src => src.ProportionalElectionListId))
            .ForMember(dst => dst.ListPosition, opts => opts.MapFrom(src => src.ProportionalElectionList.Position))
            .ForMember(dst => dst.ListNumber, opts => opts.MapFrom(src => src.ProportionalElectionList.OrderNumber))
            .ForMember(dst => dst.ListDescription, opts => opts.MapFrom(src => src.ProportionalElectionList.Description))
            .ForMember(dst => dst.ListShortDescription, opts => opts.MapFrom(src => src.ProportionalElectionList.ShortDescription));
        CreateMap<IEnumerable<DataModels.ProportionalElectionCandidate>, ProtoModels.ProportionalElectionCandidates>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(src => src));

        CreateMap<DataModels.ProportionalElectionListUnion, ProtoModels.ProportionalElectionListUnion>();
    }
}
