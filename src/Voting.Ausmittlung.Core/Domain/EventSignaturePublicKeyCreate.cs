// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class EventSignaturePublicKeyCreate
{
    public EventSignaturePublicKeyCreate()
    {
        HostId = string.Empty;
        KeyId = string.Empty;
        PublicKey = Array.Empty<byte>();
        AuthenticationTag = Array.Empty<byte>();
        HsmSignature = Array.Empty<byte>();
    }

    public int SignatureVersion { get; internal set; }

    public Guid ContestId { get; internal set; }

    public string HostId { get; internal set; }

    public string KeyId { get; internal set; }

    public byte[] PublicKey { get; internal set; }

    public DateTime ValidFrom { get; internal set; }

    public DateTime ValidTo { get; internal set; }

    public byte[] AuthenticationTag { get; internal set; }

    public byte[] HsmSignature { get; internal set; }
}
