// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ContactPersonProfile : Profile
{
    public ContactPersonProfile()
    {
        // read
        CreateMap<DataModels.ContactPerson, ProtoModels.ContactPerson>();

        // write request
        CreateMap<EnterContactPersonRequest, ContactPerson>();
    }
}
