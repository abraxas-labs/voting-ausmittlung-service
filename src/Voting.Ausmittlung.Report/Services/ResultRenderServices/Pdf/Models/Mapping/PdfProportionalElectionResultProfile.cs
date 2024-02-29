// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfProportionalElectionResultProfile : Profile
{
    public PdfProportionalElectionResultProfile()
    {
        CreateMap<ProportionalElectionResult, PdfProportionalElectionResult>();
        CreateMap<ProportionalElectionListResult, PdfProportionalElectionListResult>();
        CreateMap<ProportionalElectionCandidateResult, PdfProportionalElectionCandidateResult>();
        CreateMap<ProportionalElectionCandidateVoteSourceResult, PdfProportionalElectionCandidateVoteSourceResult>();

        CreateMap<ProportionalElectionResultSubTotal, PdfProportionalElectionResultSubTotal>();
        CreateMap<ProportionalElectionListResultSubTotal, PdfProportionalElectionListResultSubTotal>();
        CreateMap<ProportionalElectionCandidateResultSubTotal, PdfProportionalElectionCandidateResultSubTotal>();
    }
}
