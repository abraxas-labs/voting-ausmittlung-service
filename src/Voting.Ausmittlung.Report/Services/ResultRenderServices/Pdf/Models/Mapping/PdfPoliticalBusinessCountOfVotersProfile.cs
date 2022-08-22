// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfPoliticalBusinessCountOfVotersProfile : Profile
{
    public PdfPoliticalBusinessCountOfVotersProfile()
    {
        CreateMap<PoliticalBusinessCountOfVoters, PdfPoliticalBusinessCountOfVoters>();
        CreateMap<PoliticalBusinessNullableCountOfVoters, PdfPoliticalBusinessCountOfVoters>()
            .ForMember(dst => dst.ConventionalBlankBallots, opts => opts.NullSubstitute(0))
            .ForMember(dst => dst.ConventionalInvalidBallots, opts => opts.NullSubstitute(0));
    }
}
