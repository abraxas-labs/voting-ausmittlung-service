// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.EventSignature.Models;

public class PublicKeySignatureCreateData
{
    public PublicKeySignatureCreateData(
        string keyId,
        int signatureVersion,
        Guid contestId,
        string hostId,
        byte[] authenticationTag,
        byte[] hsmSignature)
    {
        KeyId = keyId;
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        AuthenticationTag = authenticationTag;
        HsmSignature = hsmSignature;
    }

    public string KeyId { get; }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public byte[] AuthenticationTag { get; }

    public byte[] HsmSignature { get; }
}
