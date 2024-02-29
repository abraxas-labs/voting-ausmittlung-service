// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.EventSignature.Models;

public class EventSignatureBusinessMetadata
{
    public EventSignatureBusinessMetadata(Guid contestId)
    {
        ContestId = contestId;
    }

    public EventSignatureBusinessMetadata(Guid contestId, int signatureVersion, string hostId, string keyId, byte[] signature)
    {
        ContestId = contestId;
        SignatureVersion = signatureVersion;
        HostId = hostId;
        KeyId = keyId;
        Signature = signature;
    }

    public Guid ContestId { get; }

    public int SignatureVersion { get; }

    public string HostId { get; } = string.Empty;

    public string KeyId { get; } = string.Empty;

    public byte[] Signature { get; } = Array.Empty<byte>();
}
