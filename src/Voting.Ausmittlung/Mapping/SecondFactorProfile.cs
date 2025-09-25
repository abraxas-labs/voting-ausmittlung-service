// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using AutoMapper;
using Voting.Ausmittlung.Core.Models;

namespace Voting.Ausmittlung.Mapping;

public class SecondFactorProfile : Profile
{
    public SecondFactorProfile()
    {
        CreateMap<SecondFactorInfo, SecondFactorTransaction>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.Transaction.Id));
    }
}
