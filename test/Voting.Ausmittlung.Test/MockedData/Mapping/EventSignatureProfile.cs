// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using ProtoBasisEvents = Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

public class EventSignatureProfile : Profile
{
    public EventSignatureProfile()
    {
        CreateMap<EventSignaturePublicKeyCreate, ProtoBasisEvents.EventSignaturePublicKeyCreated>();
        CreateMap<EventSignaturePublicKeyDelete, ProtoBasisEvents.EventSignaturePublicKeyDeleted>();
    }
}
