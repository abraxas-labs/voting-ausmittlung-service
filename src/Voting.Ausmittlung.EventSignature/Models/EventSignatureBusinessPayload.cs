// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.EventSignature.Models;

public class EventSignatureBusinessPayload
{
    public EventSignatureBusinessPayload(
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
        using var byteConverter = new ByteConverter();
        return byteConverter
            .Append(SignatureVersion)
            .Append(EventId.ToString())
            .Append(StreamName)
            .Append(EventData)
            .Append(ContestId.ToString())
            .Append(HostId)
            .Append(KeyId)
            .Append(Timestamp)
            .GetBytes();
    }
}
