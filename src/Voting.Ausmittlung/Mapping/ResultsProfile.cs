// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ResultsProfile : Profile
{
    public ResultsProfile()
    {
        // read
        CreateMap<DataModels.ResultOverview, ProtoModels.ResultOverview>();
        CreateMap<KeyValuePair<DataModels.CountingCircle, List<DataModels.SimpleCountingCircleResult>>, ProtoModels.ResultOverviewCountingCircleResults>()
            .ForMember(dst => dst.CountingCircle, opts => opts.MapFrom(src => src.Key))
            .ForMember(dst => dst.Results, opts => opts.MapFrom(src => src.Value));
        CreateMap<DataModels.SimpleCountingCircleResult, ProtoModels.ResultOverviewCountingCircleResult>()
            .ForMember(
                dst => dst.CountingCircleId,
                opts => opts.MapFrom(src => src.CountingCircle!.BasisCountingCircleId));
        CreateMap<DataModels.ResultList, ProtoModels.ResultList>();
        CreateMap<DataModels.SimpleCountingCircleResult, ProtoModels.ResultListResult>();
        CreateMap<DataModels.DomainOfInfluenceCantonDefaultsVotingCardChannel, ProtoModels.VotingCardChannel>();
        CreateMap<DataModels.CountingCircleResultComment, ProtoModels.Comment>();
    }
}
