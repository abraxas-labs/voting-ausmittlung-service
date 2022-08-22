// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class VoteProfile : Profile
{
    public VoteProfile()
    {
        // read
        CreateMap<DataModels.Vote, ProtoModels.Vote>()
            .ForMember(dst => dst.DomainOfInfluenceId, opts => opts.MapFrom(src => src.DomainOfInfluence.BasisDomainOfInfluenceId));
        CreateMap<DataModels.Ballot, ProtoModels.Ballot>();
        CreateMap<DataModels.BallotQuestion, ProtoModels.BallotQuestion>();
        CreateMap<DataModels.TieBreakQuestion, ProtoModels.TieBreakQuestion>();
    }
}
