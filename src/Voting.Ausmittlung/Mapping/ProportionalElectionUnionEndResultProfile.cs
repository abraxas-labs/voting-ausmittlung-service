// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ProportionalElectionUnionEndResultProfile : Profile
{
    public ProportionalElectionUnionEndResultProfile()
    {
        // read
        CreateMap<DataModels.ProportionalElectionUnionEndResult, ProtoModels.ProportionalElectionUnionEndResult>()
            .ForMember(
                dst => dst.ProportionalElectionEndResults,
                opts => opts.MapFrom(src => src.ProportionalElectionUnion.ProportionalElectionUnionEntries.Select(e => e.ProportionalElection.EndResult)))
            .ForMember(dst => dst.Contest, opts => opts.MapFrom(src => src.ProportionalElectionUnion.Contest));
    }
}
