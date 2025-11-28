// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfMajorityElectionProfile : Profile
{
    public PdfMajorityElectionProfile()
    {
        CreateMap<MajorityElection, PdfMajorityElection>();
        CreateMap<MajorityElection, PdfPoliticalBusiness>();
        CreateMap<MajorityElectionCandidateBase, PdfMajorityElectionCandidate>()
            .ForMember(dst => dst.Party, opts => opts.MapFrom(src => src.PartyShortDescription));

        CreateMap<SecondaryMajorityElection, PdfMajorityElection>()
            .ForMember(dst => dst.MandateAlgorithm, opt => opt.MapFrom(src => src.PrimaryMajorityElection.MandateAlgorithm))
            .ForMember(dst => dst.EmptyVoteCountDisabled, opt => opt.MapFrom(_ => false));
    }
}
