// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfCountingCircleResultProfile : Profile
{
    public PdfCountingCircleResultProfile()
    {
        CreateMap<CountingCircleResult, PdfCountingCircleResult>()
            .ForMember(
                dst => dst.IsAuditedTentativelyOrPlausibilised,
                opts => opts.MapFrom(src => src.State == CountingCircleResultState.AuditedTentatively || src.State == CountingCircleResultState.Plausibilised))
            .IncludeAllDerived();
    }
}
