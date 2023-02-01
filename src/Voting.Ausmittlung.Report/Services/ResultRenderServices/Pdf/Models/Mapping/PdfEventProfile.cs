// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfEventProfile : Profile
{
    public PdfEventProfile()
    {
        CreateMap<EventLogTenant, PdfEventTenant>();
        CreateMap<EventLogUser, PdfEventUser>();
        CreateMap<CountingCircle, PdfEventCountingCircle>();
        CreateMap<EventLogPublicKeyData, PdfEventPublicKeyData>();
        CreateMap<EventLog, PdfEventPoliticalBusiness>()
            .ForMember(dst => dst.Description, opts => opts.MapFrom(src => src.Translations.Count == 0 ? null : src.Translations.First().PoliticalBusinessDescription))
            .ForMember(dst => dst.Number, opts => opts.MapFrom(src => src.PoliticalBusinessNumber));
    }
}
