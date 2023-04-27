// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Messaging.Messages;
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

        // write
        CreateMap<UpdateResultExportConfigurationRequest, ResultExportConfiguration>();
        CreateMap<UpdatePoliticalBusinessExportMetadataRequest, ResultExportConfigurationPoliticalBusinessMetadata>();
        CreateMap<GenerateResultBundleReviewExportRequest, BundleReviewExportRequest>()
            .ForPath(dst => dst.Template.Key, opts => opts.MapFrom(x => x.TemplateKey));
    }
}
