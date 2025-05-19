// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Voting.Lib.Eventing.Persistence;
using AusmittlungEvents = Abraxas.Voting.Ausmittlung.Events.V1;
using AusmittlungEventsMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using BasisEvents = Abraxas.Voting.Basis.Events.V1;
using BasisEventsMetadata = Abraxas.Voting.Basis.Events.V1.Metadata;

namespace Voting.Ausmittlung.Report.EventLogs;

public class MetadataDescriptorProvider : IMetadataDescriptorProvider
{
    private const string AusmittlungEventNamespace = "abraxas.voting.ausmittlung.events";
    private const string BasisEventNamespace = "abraxas.voting.basis.events";

    public MessageDescriptor? GetMetadataDescriptor(IMessage eventMessage)
    {
        if (eventMessage.Descriptor.FullName == AusmittlungEvents.EventSignaturePublicKeyCreated.Descriptor.FullName
            || eventMessage.Descriptor.FullName == AusmittlungEvents.EventSignaturePublicKeyDeleted.Descriptor.FullName)
        {
            return AusmittlungEventsMetadata.EventSignaturePublicKeyMetadata.Descriptor;
        }

        if (eventMessage.Descriptor.FullName == BasisEvents.EventSignaturePublicKeyCreated.Descriptor.FullName
            || eventMessage.Descriptor.FullName == BasisEvents.EventSignaturePublicKeyDeleted.Descriptor.FullName)
        {
            return BasisEventsMetadata.EventSignaturePublicKeyMetadata.Descriptor;
        }

        if (IsAusmittlungEvent(eventMessage.Descriptor))
        {
            return AusmittlungEventsMetadata.EventSignatureBusinessMetadata.Descriptor;
        }

        if (IsBasisEvent(eventMessage.Descriptor))
        {
            return BasisEventsMetadata.EventSignatureBusinessMetadata.Descriptor;
        }

        throw new ArgumentException($"Message is not a basis or ausmittlung event {eventMessage}");
    }

    private static bool IsAusmittlungEvent(IDescriptor descriptor)
        => descriptor.FullName.StartsWith(AusmittlungEventNamespace, StringComparison.Ordinal);

    private static bool IsBasisEvent(IDescriptor descriptor)
        => descriptor.FullName.StartsWith(BasisEventNamespace, StringComparison.Ordinal);
}
