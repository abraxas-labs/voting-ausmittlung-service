// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.EventSignature.Models;

public class PublicKeySignatureData
{
    public PublicKeySignatureData(
        string keyId,
        int signatureVersion,
        Guid contestId,
        string hostId,
        byte[] hsmSignature)
    {
        KeyId = keyId;
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        HsmSignature = hsmSignature;
    }

    public string KeyId { get; }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public byte[] HsmSignature { get; }
}
