// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfProportionalElectionResultBundleProfile : Profile
{
    public PdfProportionalElectionResultBundleProfile()
    {
        CreateMap<ProportionalElectionResultBundle, PdfProportionalElectionResultBundle>();
        CreateMap<ProportionalElectionResultBallot, PdfProportionalElectionResultBallot>()
            .ForMember(
                dst => dst.AllOriginalCandidatesRemovedFromList,
                opts => opts.MapFrom(src => src.BallotCandidates.Any(c => c.OnList) && src.BallotCandidates.Where(c => c.OnList).All(c => c.RemovedFromList)));
        CreateMap<ProportionalElectionResultBallotCandidate, PdfProportionalElectionResultBallotCandidate>();
    }
}
