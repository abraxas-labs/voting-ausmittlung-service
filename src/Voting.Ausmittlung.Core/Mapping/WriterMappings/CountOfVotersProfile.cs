// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Mapping.WriterMappings;

public class CountOfVotersProfile : Profile
{
    public CountOfVotersProfile()
    {
        CreateMap<PoliticalBusinessCountOfVoters, PoliticalBusinessCountOfVotersEventData>().ReverseMap();
    }
}
