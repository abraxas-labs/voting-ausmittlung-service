﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Mapping.WriterMappings;

public class EventSignatureProfile : Profile
{
    public EventSignatureProfile()
    {
        CreateMap<EventSignaturePublicKeyCreate, EventSignaturePublicKeyCreated>();
        CreateMap<EventSignaturePublicKeyDelete, EventSignaturePublicKeyDeleted>();
    }
}
