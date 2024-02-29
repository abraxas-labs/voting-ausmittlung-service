// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfMajorityElectionResultBundleProfile : Profile
{
    public PdfMajorityElectionResultBundleProfile()
    {
        CreateMap<MajorityElectionResultBundle, PdfMajorityElectionResultBundle>();
        CreateMap<MajorityElectionResultBallot, PdfMajorityElectionResultBallot>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(x => x.BallotCandidates.Select(b => b.Candidate)));
        CreateMap<SecondaryMajorityElectionResultBallot, PdfSecondaryMajorityElectionResultBallot>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(x => x.BallotCandidates.Select(b => b.Candidate)));
    }
}
