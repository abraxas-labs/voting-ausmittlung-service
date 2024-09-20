// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Controllers.Models.Export;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Export.Models;
using Voting.Ausmittlung.Report.Models;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ExportProfile : Profile
{
    public ExportProfile()
    {
        // read
        CreateMap<ResultExportTemplate, ProtoModels.DataExportTemplate>();
        CreateMap<ExportTemplateContainer<ResultExportTemplate>, ProtoModels.DataExportTemplates>();

        CreateMap<ResultExportTemplate, ProtoModels.ProtocolExport>();
        CreateMap<DataModels.ProtocolExport, ProtoModels.ProtocolExport>()
            .ForMember(dst => dst.ProtocolExportId, opts => opts.MapFrom(src => src.Id));
        CreateMap<ProtocolExportTemplate, ProtoModels.ProtocolExport>()
            .IncludeMembers(x => x.Template)
            .AfterMap((src, dst, ctx) =>
            {
                // Cannot use IncludeMembers with ProtocolExport, as then AutoMapper tries to map null values
                if (src.ProtocolExport != null)
                {
                    ctx.Mapper.Map(src.ProtocolExport, dst);
                }
            });
        CreateMap<ExportTemplateContainer<ProtocolExportTemplate>, ProtoModels.ProtocolExports>()
            .ForMember(dst => dst.ProtocolExports_, opts => opts.MapFrom(src => src.Templates));

        CreateMap<ProtocolExportStateChanged, ProtoModels.ProtocolExportStateChange>();

        CreateMap<DataModels.ResultExportConfiguration, ProtoModels.ResultExportConfiguration>()
            .ForMember(dst => dst.PoliticalBusinessIds, opts => opts.MapFrom(src => src.PoliticalBusinesses!.Select(x => x.PoliticalBusinessId)))
            .ForMember(dst => dst.PoliticalBusinessMetadata, opts => opts.MapFrom(src => src.PoliticalBusinessMetadata!.ToDictionary(x => x.PoliticalBusinessId)));
        CreateMap<DataModels.ResultExportConfigurationPoliticalBusinessMetadata, ProtoModels.PoliticalBusinessExportMetadata>();
        CreateMap<IEnumerable<DataModels.ResultExportConfiguration>, ProtoModels.ResultExportConfigurations>()
            .ForMember(dst => dst.Configurations, opts => opts.MapFrom(x => x));

        CreateMap<ExportTemplateContainer<ResultExportTemplate>, ListDataExportsResponse>();
        CreateMap<ExportTemplateContainer<ProtocolExportTemplate>, ListProtocolExportsResponse>()
            .ForMember(dst => dst.ProtocolExports, opts => opts.MapFrom(src => src.Templates));
        CreateMap<ExportTemplateContainer<ProtocolExportTemplate>, ListProtocolExportStatesResponse>()
            .ForMember(dst => dst.ProtocolExports, opts => opts.MapFrom(src => src.Templates.Select(t => t.ProtocolExport).WhereNotNull()));

        CreateMap<DataModels.Contest, ContestResponse>()
            .ForMember(dst => dst.DomainOfInfluenceId, opts => opts.MapFrom(src => src.DomainOfInfluence.BasisDomainOfInfluenceId));
        CreateMap<DataModels.CountingCircle, CountingCircleResponse>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.BasisCountingCircleId));
        CreateMap<DataModels.ProtocolExport, ProtocolExportResponse>();
        CreateMap<DataModels.ProtocolExport, ProtocolExportStateResponse>()
            .ForMember(dst => dst.ProtocolExportId, opts => opts.MapFrom(src => src.Id));
        CreateMap<ResultExportTemplate, DataExportTemplate>();
        CreateMap<ResultExportTemplate, ProtocolExportResponse>();
        CreateMap<ProtocolExportTemplate, ProtocolExportResponse>()
            .ForMember(dst => dst.ProtocolExportId, opts => opts.MapFrom(src => src.ProtocolExport != null ? (Guid?)src.ProtocolExport.Id : null))
            .IncludeMembers(src => src.Template)
            .AfterMap((src, dst, ctx) =>
            {
                // Cannot use IncludeMembers with ProtocolExport, as then AutoMapper tries to map null values
                if (src.ProtocolExport != null)
                {
                    ctx.Mapper.Map(src.ProtocolExport, dst);
                }
            });

        CreateMap<DownloadEch0252ExportRequest, Ech0252FilterRequest>();

        // write
        CreateMap<UpdateResultExportConfigurationRequest, ResultExportConfiguration>();
        CreateMap<UpdatePoliticalBusinessExportMetadataRequest, ResultExportConfigurationPoliticalBusinessMetadata>();
    }
}
