// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfVoteDomainOfInfluenceResultProfile : Profile
{
    public PdfVoteDomainOfInfluenceResultProfile()
    {
        CreateMap<IGrouping<Ballot, VoteDomainOfInfluenceBallotResult>, PdfVoteBallotDomainOfInfluenceResult>()
            .ForMember(dst => dst.Ballot, opts => opts.MapFrom(src => src.Key))
            .ForMember(dst => dst.Results, opts => opts.MapFrom(src => src));
        CreateMap<VoteDomainOfInfluenceBallotResult, PdfVoteDomainOfInfluenceBallotResult>();
        CreateMap<VoteCountingCircleBallotResult, PdfVoteCountingCircleBallotResult>();
    }
}
