// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfPoliticalBusinessCountOfVotersProfile : Profile
{
    public PdfPoliticalBusinessCountOfVotersProfile()
    {
        CreateMap<PoliticalBusinessCountOfVoters, PdfPoliticalBusinessCountOfVoters>()
            .ForMember(dst => dst.EVotingTotalUnaccountedBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.BlankBallots + src.EVotingSubTotal.InvalidBallots))
            .ForMember(dst => dst.EVotingAccountedBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.AccountedBallots))
            .ForMember(dst => dst.EVotingBlankBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.BlankBallots))
            .ForMember(dst => dst.EVotingInvalidBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.InvalidBallots))
            .ForMember(dst => dst.EVotingReceivedBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.ReceivedBallots))
            .ForMember(dst => dst.ConventionalTotalUnaccountedBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.BlankBallots + src.ConventionalSubTotal.InvalidBallots))
            .ForMember(dst => dst.ConventionalAccountedBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.AccountedBallots))
            .ForMember(dst => dst.ConventionalBlankBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.BlankBallots))
            .ForMember(dst => dst.ConventionalInvalidBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.InvalidBallots))
            .ForMember(dst => dst.ConventionalReceivedBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.ReceivedBallots));
        CreateMap<PoliticalBusinessNullableCountOfVoters, PdfPoliticalBusinessCountOfVoters>()
            .ForMember(dst => dst.EVotingTotalUnaccountedBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.BlankBallots + src.EVotingSubTotal.InvalidBallots))
            .ForMember(dst => dst.EVotingAccountedBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.AccountedBallots))
            .ForMember(dst => dst.EVotingBlankBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.BlankBallots))
            .ForMember(dst => dst.EVotingInvalidBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.InvalidBallots))
            .ForMember(dst => dst.EVotingReceivedBallots, opts => opts.MapFrom(src => src.EVotingSubTotal.ReceivedBallots))
            .ForMember(dst => dst.ConventionalTotalUnaccountedBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.BlankBallots.GetValueOrDefault() + src.ConventionalSubTotal.InvalidBallots.GetValueOrDefault()))
            .ForMember(dst => dst.ConventionalAccountedBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.AccountedBallots))
            .ForMember(dst => dst.ConventionalBlankBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.BlankBallots))
            .ForMember(dst => dst.ConventionalInvalidBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.InvalidBallots))
            .ForMember(dst => dst.ConventionalReceivedBallots, opts => opts.MapFrom(src => src.ConventionalSubTotal.ReceivedBallots));
    }
}
