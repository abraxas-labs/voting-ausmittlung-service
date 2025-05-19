// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class MajorityElectionProfile : Profile
{
    public MajorityElectionProfile()
    {
        // Map secondary election to primary election, as they are treated the same in the pdf context.
        CreateMap<SecondaryMajorityElection, MajorityElection>()
            .ForMember(dst => dst.MandateAlgorithm, opts => opts.MapFrom(src => src.PrimaryMajorityElection.MandateAlgorithm))
            .ForMember(dst => dst.PoliticalBusinessTranslations, opts => opts.Ignore())
            .ForMember(dst => dst.EndResult, opts => opts.Ignore())
            .ForMember(dst => dst.Results, opts => opts.Ignore());

        CreateMap<SecondaryMajorityElectionTranslation, MajorityElectionTranslation>();
        CreateMap<SecondaryMajorityElectionCandidateResult, MajorityElectionCandidateResult>()
            .ForMember(dst => dst.ElectionResult, opts => opts.Ignore());
        CreateMap<SecondaryMajorityElectionCandidateEndResult, MajorityElectionCandidateEndResult>();
        CreateMap<SecondaryMajorityElectionCandidate, MajorityElectionCandidate>();
        CreateMap<SecondaryMajorityElectionCandidateTranslation, MajorityElectionCandidateTranslation>();
    }
}
