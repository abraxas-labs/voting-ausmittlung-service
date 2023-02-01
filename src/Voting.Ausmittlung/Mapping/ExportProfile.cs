// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Report.Models;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ExportProfile : Profile
{
    public ExportProfile()
    {
        // read
        CreateMap<ResultExportTemplate, ProtoModels.ResultExportTemplate>();
        CreateMap<ResultExportTemplateContainer, ProtoModels.ResultExportTemplates>();

        CreateMap<DataModels.ResultExportConfiguration, ProtoModels.ResultExportConfiguration>()
            .ForMember(dst => dst.PoliticalBusinessIds, opts => opts.MapFrom(src => src.PoliticalBusinesses!.Select(x => x.PoliticalBusinessId)))
            .ForMember(dst => dst.PoliticalBusinessMetadata, opts => opts.MapFrom(src => src.PoliticalBusinessMetadata!.ToDictionary(x => x.PoliticalBusinessId)));
        CreateMap<DataModels.ResultExportConfigurationPoliticalBusinessMetadata, ProtoModels.PoliticalBusinessExportMetadata>();
        CreateMap<IEnumerable<DataModels.ResultExportConfiguration>, ProtoModels.ResultExportConfigurations>()
            .ForMember(dst => dst.Configurations, opts => opts.MapFrom(x => x));

        // write
        CreateMap<UpdateResultExportConfigurationRequest, ResultExportConfiguration>();
        CreateMap<UpdatePoliticalBusinessExportMetadataRequest, ResultExportConfigurationPoliticalBusinessMetadata>();
        CreateMap<GenerateResultExportRequest, ResultExportRequest>()
            .ForPath(dst => dst.Template.Key, opts => opts.MapFrom(x => x.Key));
        CreateMap<GenerateResultBundleReviewExportRequest, ResultExportRequest>()
            .ForPath(dst => dst.Template.Key, opts => opts.MapFrom(x => x.TemplateKey))
            .ForMember(dst => dst.PoliticalBusinessIds, opts => opts.MapFrom(x => new List<Guid> { x.PoliticalBusinessId }));
    }
}
