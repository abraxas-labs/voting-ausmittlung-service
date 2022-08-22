// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class VoteResultProfile : Profile
{
    public VoteResultProfile()
    {
        CreateMap<VoteResultEntryParamsEventData, VoteResultEntryParams>();
    }
}
