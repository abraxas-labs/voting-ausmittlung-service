// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using AusmittlungEventsMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using BasisEventsMetadata = Abraxas.Voting.Basis.Events.V1.Metadata;

namespace Voting.Ausmittlung.Report.EventLogs;

internal static class AppDescriptorProvider
{
    private const string AusmittlungEventNamespace = "abraxas.voting.ausmittlung.events";
    private const string BasisEventNamespace = "abraxas.voting.basis.events";

    public static IDescriptor GetPublicKeyMetadataDescriptor(IMessage data)
    {
        if (IsAusmittlungEvent(data.Descriptor))
        {
            return AusmittlungEventsMetadata.EventSignaturePublicKeyMetadata.Descriptor;
        }

        if (IsBasisEvent(data.Descriptor))
        {
            return BasisEventsMetadata.EventSignaturePublicKeyMetadata.Descriptor;
        }

        throw new ArgumentException($"Message is not a basis or ausmittlung event {data}");
    }

    public static IDescriptor GetBusinessMetadataDescriptor(IMessage data)
    {
        if (IsAusmittlungEvent(data.Descriptor))
        {
            return AusmittlungEventsMetadata.EventSignatureBusinessMetadata.Descriptor;
        }

        if (IsBasisEvent(data.Descriptor))
        {
            return BasisEventsMetadata.EventSignatureBusinessMetadata.Descriptor;
        }

        throw new ArgumentException($"Message is not a basis or ausmittlung event {data}");
    }

    private static bool IsAusmittlungEvent(IDescriptor descriptor)
        => descriptor.FullName.StartsWith(AusmittlungEventNamespace, StringComparison.Ordinal);

    private static bool IsBasisEvent(IDescriptor descriptor)
        => descriptor.FullName.StartsWith(BasisEventNamespace, StringComparison.Ordinal);
}
