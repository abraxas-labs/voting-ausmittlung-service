// (c) Copyright 2022 by Abraxas Informatik AG
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
        CreateMap<TieBreakQuestionResult, PdfTieBreakQuestionResult>();
        CreateMap<TieBreakQuestionDomainOfInfluenceResult, PdfTieBreakQuestionResult>();
    }
}
