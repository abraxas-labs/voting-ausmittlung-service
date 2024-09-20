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
            .ForMember(dst => dst.EVotingTotalUnaccountedBallots, opts => opts.MapFrom(src => src.EVotingBlankBallots + src.EVotingInvalidBallots))
            .ForMember(dst => dst.ConventionalTotalUnaccountedBallots, opts => opts.MapFrom(src => src.ConventionalBlankBallots + src.ConventionalInvalidBallots));
        CreateMap<PoliticalBusinessNullableCountOfVoters, PdfPoliticalBusinessCountOfVoters>()
            .ForMember(dst => dst.EVotingTotalUnaccountedBallots, opts => opts.MapFrom(src => src.EVotingBlankBallots + src.EVotingInvalidBallots))
            .ForMember(dst => dst.ConventionalTotalUnaccountedBallots, opts => opts.MapFrom(src => src.ConventionalBlankBallots.GetValueOrDefault() + src.ConventionalInvalidBallots.GetValueOrDefault()));
    }
}
