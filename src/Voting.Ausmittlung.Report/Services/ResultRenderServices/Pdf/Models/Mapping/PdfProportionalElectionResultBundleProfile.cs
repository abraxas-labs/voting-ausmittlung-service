// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfProportionalElectionResultBundleProfile : Profile
{
    public PdfProportionalElectionResultBundleProfile()
    {
        CreateMap<ProportionalElectionResultBundle, PdfProportionalElectionResultBundle>();
        CreateMap<ProportionalElectionResultBallot, PdfProportionalElectionResultBallot>();
        CreateMap<ProportionalElectionResultBallotCandidate, PdfProportionalElectionResultBallotCandidate>();
    }
}
