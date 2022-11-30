// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class EventSignaturePublicKeyAggregateDeleteData
{
    public EventSignaturePublicKeyAggregateDeleteData(string keyId, int signatureVersion, Guid contestId, string hostId, byte[] authenticationTag, long signedEventCount, DateTime deletedAt, byte[] hsmSignature)
    {
        KeyId = keyId;
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        AuthenticationTag = authenticationTag;
        SignedEventCount = signedEventCount;
        DeletedAt = deletedAt;
        HsmSignature = hsmSignature;
    }

    public string KeyId { get; }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public byte[] AuthenticationTag { get; }

    public long SignedEventCount { get; }

    public DateTime DeletedAt { get; }

    public byte[] HsmSignature { get; }
}
