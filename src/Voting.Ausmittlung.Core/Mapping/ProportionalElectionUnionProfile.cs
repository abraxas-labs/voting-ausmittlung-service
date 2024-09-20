// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class ProportionalElectionUnionProfile : Profile
{
    public ProportionalElectionUnionProfile()
    {
        CreateMap<ProportionalElectionUnionEventData, ProportionalElectionUnion>();
    }
}
