// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.EventSignature.Models;

public class EventSignaturePayload
{
    public EventSignaturePayload(
        int signatureVersion,
        Guid eventId,
        string streamName,
        byte[] eventData,
        Guid contestId,
        string hostId,
        string keyId,
        DateTime timestamp)
    {
        SignatureVersion = signatureVersion;
        EventId = eventId;
        StreamName = streamName;
        EventData = eventData;
        ContestId = contestId;
        HostId = hostId;
        KeyId = keyId;
        Timestamp = timestamp;
    }

    public int SignatureVersion { get; }

    public Guid EventId { get; }

    public string StreamName { get; }

    public byte[] EventData { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public string KeyId { get; }

    public DateTime Timestamp { get; }

    // changes here are event breaking and need another signature version.
    public byte[] ConvertToBytesToSign()
    {
        return ByteConverter.Concat(
            SignatureVersion,
            EventId,
            StreamName,
            EventData,
            ContestId,
            HostId,
            KeyId,
            Timestamp);
    }
}
