// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // read
        CreateMap<DataModels.User, ProtoModels.User>();
    }
}
