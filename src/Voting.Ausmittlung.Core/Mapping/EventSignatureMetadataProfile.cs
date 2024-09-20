// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.EventSignature.Models;
using ProtoEventSignatureBusinessMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureBusinessMetadata;
using ProtoEventSignaturePublicKeyMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignaturePublicKeyMetadata;

namespace Voting.Ausmittlung.Core.Mapping;

public class EventSignatureMetadataProfile : Profile
{
    public EventSignatureMetadataProfile()
    {
        CreateMap<EventSignatureBusinessMetadata, ProtoEventSignatureBusinessMetadata>();
        CreateMap<EventSignaturePublicKeyCreate, ProtoEventSignaturePublicKeyMetadata>();
        CreateMap<EventSignaturePublicKeyDelete, ProtoEventSignaturePublicKeyMetadata>();
    }
}
