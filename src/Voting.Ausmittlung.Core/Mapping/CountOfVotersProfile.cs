// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Mapping;

public class CountOfVotersProfile : Profile
{
    public CountOfVotersProfile()
    {
        CreateMap<PoliticalBusinessCountOfVotersEventData, PoliticalBusinessNullableCountOfVoters>()
            .ForPath(dst => dst.ConventionalSubTotal.AccountedBallots, opts => opts.MapFrom(src => src.ConventionalAccountedBallots))
            .ForPath(dst => dst.ConventionalSubTotal.BlankBallots, opts => opts.MapFrom(src => src.ConventionalBlankBallots))
            .ForPath(dst => dst.ConventionalSubTotal.InvalidBallots, opts => opts.MapFrom(src => src.ConventionalInvalidBallots))
            .ForPath(dst => dst.ConventionalSubTotal.ReceivedBallots, opts => opts.MapFrom(src => src.ConventionalReceivedBallots))
            .ReverseMap();
        CreateMap<PoliticalBusinessCountOfVotersEventData, PoliticalBusinessCountOfVoters>()
            .ForPath(dst => dst.ConventionalSubTotal.AccountedBallots, opts => opts.MapFrom(src => src.ConventionalAccountedBallots ?? 0))
            .ForPath(dst => dst.ConventionalSubTotal.BlankBallots, opts => opts.MapFrom(src => src.ConventionalBlankBallots ?? 0))
            .ForPath(dst => dst.ConventionalSubTotal.InvalidBallots, opts => opts.MapFrom(src => src.ConventionalInvalidBallots ?? 0))
            .ForPath(dst => dst.ConventionalSubTotal.ReceivedBallots, opts => opts.MapFrom(src => src.ConventionalReceivedBallots ?? 0))
            .ReverseMap();
        CreateMap<DomainModels.PoliticalBusinessCountOfVoters, PoliticalBusinessNullableCountOfVoters>()
            .ForPath(dst => dst.ConventionalSubTotal.AccountedBallots, opts => opts.MapFrom(src => src.ConventionalAccountedBallots))
            .ForPath(dst => dst.ConventionalSubTotal.BlankBallots, opts => opts.MapFrom(src => src.ConventionalBlankBallots))
            .ForPath(dst => dst.ConventionalSubTotal.InvalidBallots, opts => opts.MapFrom(src => src.ConventionalInvalidBallots))
            .ForPath(dst => dst.ConventionalSubTotal.ReceivedBallots, opts => opts.MapFrom(src => src.ConventionalReceivedBallots))
            .ReverseMap();
        CreateMap<DomainModels.VotingCardResultDetail, VotingCardResultDetail>();
    }
}
