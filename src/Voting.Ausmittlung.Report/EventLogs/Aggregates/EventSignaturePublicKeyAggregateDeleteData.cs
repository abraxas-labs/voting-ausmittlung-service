// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Google.Protobuf;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class EventSignaturePublicKeyAggregateDeleteData
{
    public EventSignaturePublicKeyAggregateDeleteData(
        IMessage eventData,
        string keyId,
        int signatureVersion,
        Guid contestId,
        string hostId,
        byte[] authenticationTag,
        long signedEventCount,
        DateTime deletedAt,
        byte[] hsmSignature)
    {
        EventData = eventData;
        KeyId = keyId;
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        AuthenticationTag = authenticationTag;
        SignedEventCount = signedEventCount;
        DeletedAt = deletedAt;
        HsmSignature = hsmSignature;
    }

    public IMessage EventData { get; }

    public string KeyId { get; }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public byte[] AuthenticationTag { get; }

    public long SignedEventCount { get; }

    public DateTime DeletedAt { get; }

    public byte[] HsmSignature { get; }
}
