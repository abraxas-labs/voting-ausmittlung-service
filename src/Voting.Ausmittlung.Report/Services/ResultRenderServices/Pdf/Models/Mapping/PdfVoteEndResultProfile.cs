// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfVoteEndResultProfile : Profile
{
    public PdfVoteEndResultProfile()
    {
        CreateMap<VoteEndResult, PdfVoteEndResult>();
        CreateMap<BallotEndResult, PdfBallotEndResult>();
        CreateMap<BallotQuestionEndResult, PdfBallotQuestionEndResult>();
        CreateMap<TieBreakQuestionEndResult, PdfTieBreakQuestionEndResult>();
    }
}
