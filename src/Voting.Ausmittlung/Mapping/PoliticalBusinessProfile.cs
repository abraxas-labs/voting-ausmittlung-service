// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class PoliticalBusinessProfile : Profile
{
    public PoliticalBusinessProfile()
    {
        // read
        CreateMap<DataModels.SimplePoliticalBusiness, ProtoModels.SimplePoliticalBusiness>();
    }
}
