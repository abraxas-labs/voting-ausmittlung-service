// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfMajorityElectionDomainOfInfluenceResultProfile : Profile
{
    public PdfMajorityElectionDomainOfInfluenceResultProfile()
    {
        CreateMap<MajorityElectionDomainOfInfluenceResult, PdfMajorityElectionDomainOfInfluenceResult>();
        CreateMap<MajorityElectionCandidateDomainOfInfluenceResult, PdfMajorityElectionCandidateResult>();
    }
}
