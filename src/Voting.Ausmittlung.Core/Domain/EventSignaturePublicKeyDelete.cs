// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class EventSignaturePublicKeyDelete
{
    public EventSignaturePublicKeyDelete()
    {
        HostId = string.Empty;
        KeyId = string.Empty;
        AuthenticationTag = Array.Empty<byte>();
        HsmSignature = Array.Empty<byte>();
    }

    public int SignatureVersion { get; internal set; }

    public Guid ContestId { get; internal set; }

    public string HostId { get; internal set; }

    public string KeyId { get; internal set; }

    public long SignedEventCount { get; internal set; }

    public DateTime DeletedAt { get; internal set; }

    public byte[] AuthenticationTag { get; internal set; }

    public byte[] HsmSignature { get; internal set; }
}
