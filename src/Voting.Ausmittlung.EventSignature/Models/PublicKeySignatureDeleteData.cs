// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.EventSignature.Models;

public class PublicKeySignatureDeleteData
{
    public PublicKeySignatureDeleteData(
        string keyId,
        int signatureVersion,
        Guid contestId,
        string hostId,
        long signedEventCount,
        byte[] authenticationTag,
        byte[] hsmSignature)
    {
        KeyId = keyId;
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        SignedEventCount = signedEventCount;
        AuthenticationTag = authenticationTag;
        HsmSignature = hsmSignature;
    }

    public string KeyId { get; }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public long SignedEventCount { get; }

    public byte[] AuthenticationTag { get; }

    public byte[] HsmSignature { get; }
}
