// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class ProportionalElectionResultProfile : Profile
{
    public ProportionalElectionResultProfile()
    {
        CreateMap<ProportionalElectionResultEntryParamsEventData, ProportionalElectionResultEntryParams>();
    }
}
