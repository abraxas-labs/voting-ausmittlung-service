// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class EventSignaturePublicKeyAggregateData
{
    public EventSignaturePublicKeyAggregateData(
        string keyId,
        int signatureVersion,
        Guid contestId,
        string hostId,
        byte[] hsmSignature,
        byte[] publicKey,
        DateTime validFrom,
        DateTime validTo)
    {
        KeyId = keyId;
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        HsmSignature = hsmSignature;
        PublicKey = publicKey;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public string KeyId { get; }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public byte[] HsmSignature { get; }

    public byte[] PublicKey { get; }

    public DateTime ValidFrom { get; }

    public DateTime ValidTo { get; }

    public DateTime? Deleted { get; set; }
}
