// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfVoteResultProfile : Profile
{
    public PdfVoteResultProfile()
    {
        CreateMap<VoteResult, PdfVoteResult>();
        CreateMap<BallotResult, PdfBallotResult>();
        CreateMap<BallotQuestionResult, PdfBallotQuestionResult>();
        CreateMap<BallotQuestionDomainOfInfluenceResult, PdfBallotQuestionResult>();
        CreateMap<BallotQuestionResultSubTotal, PdfBallotQuestionResultSubTotal>()
            .ForMember(dst => dst.CountOfAnswerTotal, opts => opts.MapFrom(src =>
                    src.TotalCountOfAnswerYes + src.TotalCountOfAnswerNo + src.TotalCountOfAnswerUnspecified))
            .ForMember(dst => dst.PercentageYes, opts => opts.MapFrom(src =>
                src.TotalCountOfAnswerYes == 0 && src.TotalCountOfAnswerNo == 0
                    ? 0
                    : (decimal)src.TotalCountOfAnswerYes / (src.TotalCountOfAnswerYes + src.TotalCountOfAnswerNo)))
            .ForMember(dst => dst.PercentageNo, opts => opts.MapFrom((_, dst) => 1 - dst.PercentageYes));
        CreateMap<BallotQuestionResultNullableSubTotal, PdfBallotQuestionResultSubTotal>()
            .ForMember(dst => dst.CountOfAnswerTotal, opts => opts.MapFrom(src =>
                src.TotalCountOfAnswerYes.GetValueOrDefault() + src.TotalCountOfAnswerNo.GetValueOrDefault() + src.TotalCountOfAnswerUnspecified.GetValueOrDefault()))
            .ForMember(dst => dst.PercentageYes, opts => opts.MapFrom(src =>
                src.TotalCountOfAnswerYes.GetValueOrDefault() == 0 && src.TotalCountOfAnswerNo.GetValueOrDefault() == 0
                    ? 0
                    : (decimal)src.TotalCountOfAnswerYes.GetValueOrDefault() / (src.TotalCountOfAnswerYes.GetValueOrDefault() + src.TotalCountOfAnswerNo.GetValueOrDefault())))
            .ForMember(dst => dst.PercentageNo, opts => opts.MapFrom((_, dst) => 1 - dst.PercentageYes));
        CreateMap<TieBreakQuestionResult, PdfTieBreakQuestionResult>();
        CreateMap<TieBreakQuestionDomainOfInfluenceResult, PdfTieBreakQuestionResult>();
        CreateMap<TieBreakQuestionResultSubTotal, PdfTieBreakQuestionResultSubTotal>()
            .ForMember(dst => dst.CountOfAnswerTotal, opts => opts.MapFrom(src => src.TotalCountOfAnswerQ1 + src.TotalCountOfAnswerQ2 + src.TotalCountOfAnswerUnspecified))
            .ForMember(dst => dst.PercentageQ1, opts => opts.MapFrom(src =>
                src.TotalCountOfAnswerQ1 == 0 && src.TotalCountOfAnswerQ2 == 0
                    ? 0
                    : (decimal)src.TotalCountOfAnswerQ1 / (src.TotalCountOfAnswerQ1 + src.TotalCountOfAnswerQ2)))
            .ForMember(dst => dst.PercentageQ2, opts => opts.MapFrom((_, dst) => 1 - dst.PercentageQ1));
        CreateMap<TieBreakQuestionResultNullableSubTotal, PdfTieBreakQuestionResultSubTotal>()
            .ForMember(dst => dst.CountOfAnswerTotal, opts => opts.MapFrom(src => src.TotalCountOfAnswerQ1.GetValueOrDefault() + src.TotalCountOfAnswerQ2.GetValueOrDefault() + src.TotalCountOfAnswerUnspecified.GetValueOrDefault()))
            .ForMember(dst => dst.PercentageQ1, opts => opts.MapFrom(src =>
                src.TotalCountOfAnswerQ1.GetValueOrDefault() == 0 && src.TotalCountOfAnswerQ2.GetValueOrDefault() == 0
                    ? 0
                    : (decimal)src.TotalCountOfAnswerQ1.GetValueOrDefault() / (src.TotalCountOfAnswerQ1.GetValueOrDefault() + src.TotalCountOfAnswerQ2.GetValueOrDefault())))
            .ForMember(dst => dst.PercentageQ2, opts => opts.MapFrom((_, dst) => 1 - dst.PercentageQ1));
    }
}
