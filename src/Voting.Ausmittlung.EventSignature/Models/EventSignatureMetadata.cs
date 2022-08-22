// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.EventSignature.Models;

public class EventSignatureMetadata
{
    public EventSignatureMetadata(Guid contestId)
    {
        ContestId = contestId;
    }

    public EventSignatureMetadata(Guid contestId, int signatureVersion, string hostId, string keyId, byte[] signature)
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
