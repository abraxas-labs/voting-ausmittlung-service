// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfMajorityElectionEndResultProfile : Profile
{
    public PdfMajorityElectionEndResultProfile()
    {
        CreateMap<MajorityElectionEndResult, PdfMajorityElectionEndResult>();
        CreateMap<MajorityElectionEndResultCalculation, PdfMajorityElectionEndResultCalculation>();
        CreateMap<MajorityElectionCandidateEndResult, PdfMajorityElectionCandidateEndResult>();

        CreateMap<SecondaryMajorityElectionEndResult, PdfMajorityElectionEndResult>()
            .ForMember(dst => dst.CountOfVoters, opts => opts.MapFrom(src => src.PrimaryMajorityElectionEndResult.CountOfVoters))
            .ForMember(dst => dst.AllCountingCirclesDone, opts => opts.MapFrom(src => src.PrimaryMajorityElectionEndResult.AllCountingCirclesDone))
            .ForMember(dst => dst.TotalVoteCount, opts => opts.MapFrom(src => src.PrimaryMajorityElectionEndResult.TotalVoteCount))
            .ForMember(dst => dst.TotalCountOfCountingCircles, opts => opts.MapFrom(src => src.PrimaryMajorityElectionEndResult.TotalCountOfCountingCircles))
            .ForMember(dst => dst.CountOfDoneCountingCircles, opts => opts.MapFrom(src => src.PrimaryMajorityElectionEndResult.CountOfDoneCountingCircles))
            .ForMember(dst => dst.TotalCountOfVoters, opts => opts.MapFrom(src => src.PrimaryMajorityElectionEndResult.TotalCountOfVoters));
        CreateMap<SecondaryMajorityElectionCandidateEndResult, PdfMajorityElectionCandidateEndResult>();
    }
}
