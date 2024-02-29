// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ResultCommentProfile : Profile
{
    public ResultCommentProfile()
    {
        // read
        CreateMap<DataModels.CountingCircleResultComment, ProtoModels.Comment>();
        CreateMap<IEnumerable<DataModels.CountingCircleResultComment>, ProtoModels.Comments>()
            .ForMember(dst => dst.Comments_, opts => opts.MapFrom(src => src));
    }
}
