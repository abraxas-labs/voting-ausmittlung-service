// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.EventSignature.Models;
using ProtoEventSignatureMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureMetadata;

namespace Voting.Ausmittlung.Core.Mapping;

public class EventSignatureMetadataProfile : Profile
{
    public EventSignatureMetadataProfile()
    {
        CreateMap<EventSignatureMetadata, ProtoEventSignatureMetadata>();
    }
}
