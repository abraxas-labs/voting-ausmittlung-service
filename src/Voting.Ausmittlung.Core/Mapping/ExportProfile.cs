// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class ExportProfile : Profile
{
    public ExportProfile()
    {
        CreateMap<ExportConfigurationEventData, ExportConfiguration>()
            .ForMember(dst => dst.SedexSenderId, opts =>
            {
                opts.PreCondition(src => src.SedexConfiguration != null);
                opts.MapFrom(src => src.SedexConfiguration!.SenderId);
            })
            .ForMember(dst => dst.SedexRecipientIds, opts =>
            {
                opts.PreCondition(src => src.SedexConfiguration != null);
                opts.MapFrom(src => src.SedexConfiguration!.RecipientIds);
            });

        CreateMap<ProtocolExportStarted, ProtocolExport>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.ProtocolExportId));
    }
}
