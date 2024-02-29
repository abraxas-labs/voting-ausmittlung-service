// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Google.Protobuf;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class EventSignaturePublicKeyAggregateCreateData
{
    public EventSignaturePublicKeyAggregateCreateData(
        IMessage eventData,
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
        EventData = eventData;
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

    public IMessage EventData { get; }

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
