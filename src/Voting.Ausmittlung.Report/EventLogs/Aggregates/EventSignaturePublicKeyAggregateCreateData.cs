// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class EventSignaturePublicKeyAggregateCreateData
{
    public EventSignaturePublicKeyAggregateCreateData(
        string keyId,
        int signatureVersion,
        Guid contestId,
        string hostId,
        byte[] authenticationTag,
        byte[] publicKey,
        DateTime validFrom,
        DateTime validTo,
        byte[] hsmSignature)
    {
        KeyId = keyId;
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        AuthenticationTag = authenticationTag;
        PublicKey = publicKey;
        ValidFrom = validFrom;
        ValidTo = validTo;
        HsmSignature = hsmSignature;
    }

    public string KeyId { get; }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public byte[] AuthenticationTag { get; }

    public byte[] PublicKey { get; }

    public DateTime ValidFrom { get; }

    public DateTime ValidTo { get; }

    public byte[] HsmSignature { get; }
}
