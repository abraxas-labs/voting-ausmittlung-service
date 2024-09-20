// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using BasisEvents = Abraxas.Voting.Basis.Events.V1.Data;

namespace Voting.Ausmittlung.Core.Mapping;

public class ContactPersonProfile : Profile
{
    public ContactPersonProfile()
    {
        CreateMap<ContactPersonEventData, ContactPerson>();
        CreateMap<BasisEvents.ContactPersonEventData, ContactPerson>();
        CreateMap<ContactPerson, ContactPerson>();
    }
}
