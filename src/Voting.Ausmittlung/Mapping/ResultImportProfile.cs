// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using DomainModels = Voting.Ausmittlung.Core.Domain;
using ImportModels = Voting.Ausmittlung.Core.Models.Import;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ResultImportProfile : Profile
{
    public ResultImportProfile()
    {
        // read
        CreateMap<DataModels.ResultImport, ProtoModels.ResultImport>();
        CreateMap<IEnumerable<DataModels.ResultImport>, ProtoModels.ResultImports>()
            .ForMember(dst => dst.Imports, opts => opts.MapFrom(x => x));
        CreateMap<DataModels.IgnoredImportCountingCircle, ProtoModels.ResultImportIgnoredCountingCircle>();
        CreateMap<DataModels.ResultImportCountingCircle, ProtoModels.CountingCircle>()
            .IncludeMembers(x => x.CountingCircle)
            .ForMember(dst => dst.Id, opts => opts.MapFrom(x => x.CountingCircle!.BasisCountingCircleId));

        CreateMap<ImportModels.ImportMajorityElectionWriteInMappings, ProtoModels.MajorityElectionContestWriteInMappings>();
        CreateMap<ImportModels.MajorityElectionGroupedWriteInMappings, ProtoModels.MajorityElectionWriteInMappings>()
            .ForMember(dst => dst.InvalidVotes, opts => opts.MapFrom(x => x.Election.Contest.CantonDefaults.MajorityElectionInvalidVotes))
            .ForMember(dst => dst.IndividualVotes, opts => opts.MapFrom(x => !x.Election.IndividualCandidatesDisabled));
        CreateMap<DataModels.MajorityElectionWriteInMappingBase, ProtoModels.MajorityElectionWriteInMapping>();

        // write
        CreateMap<MapMajorityElectionWriteInRequest, DomainModels.MajorityElectionWriteIn>();
    }
}
