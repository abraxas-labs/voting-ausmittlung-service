// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfMajorityElectionResultProfile : Profile
{
    public PdfMajorityElectionResultProfile()
    {
        CreateMap<MajorityElectionResult, PdfMajorityElectionResult>();
        CreateMap<MajorityElectionCandidateResult, PdfMajorityElectionCandidateResult>();
        CreateMap<MajorityElectionResultSubTotal, PdfMajorityElectionResultSubTotal>();
        CreateMap<MajorityElectionResultNullableSubTotal, PdfMajorityElectionResultSubTotal>();

        CreateMap<SecondaryMajorityElectionResult, PdfMajorityElectionResult>()
            .ForMember(dst => dst.CountingCircle, opts => opts.MapFrom(src => src.PrimaryResult.CountingCircle))
            .ForMember(dst => dst.CountOfVoters, opts => opts.MapFrom(src => src.PrimaryResult.CountOfVoters))
            .ForMember(dst => dst.TotalCountOfVoters, opts => opts.MapFrom(src => src.PrimaryResult.TotalCountOfVoters))
            .ForMember(dst => dst.State, opts => opts.MapFrom(src => src.PrimaryResult.State))
            .ForMember(
                dst => dst.IsAuditedTentativelyOrPlausibilised,
                opts => opts.MapFrom(src =>
                    src.PrimaryResult.State == CountingCircleResultState.AuditedTentatively ||
                    src.PrimaryResult.State == CountingCircleResultState.Plausibilised));
        CreateMap<SecondaryMajorityElectionCandidateResult, PdfMajorityElectionCandidateResult>();
    }
}
