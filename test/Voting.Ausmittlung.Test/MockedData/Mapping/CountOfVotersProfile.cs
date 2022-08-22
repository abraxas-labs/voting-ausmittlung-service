// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

public class CountOfVotersProfile : Profile
{
    public CountOfVotersProfile()
    {
        CreateMap<EnterPoliticalBusinessCountOfVotersRequest, PoliticalBusinessNullableCountOfVoters>();
    }
}
