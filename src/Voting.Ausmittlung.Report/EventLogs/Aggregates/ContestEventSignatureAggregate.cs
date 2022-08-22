// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class ContestEventSignatureAggregate
{
    private readonly Dictionary<string, EventSignaturePublicKeyAggregateData> _publicKeyAggregateDataByKeyId = new();

    public EventSignaturePublicKeyAggregateData? GetPublicKeyAggregateData(string keyId)
        => _publicKeyAggregateDataByKeyId.GetValueOrDefault(keyId);

    public void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case EventSignaturePublicKeySigned e:
                Apply(e);
                break;
            case EventSignaturePublicKeyDeleted e:
                Apply(e);
                break;
        }
    }

    private void Apply(EventSignaturePublicKeySigned ev)
    {
        var signature = new EventSignaturePublicKeyAggregateData(
            ev.KeyId,
            ev.SignatureVersion,
            Guid.Parse(ev.ContestId),
            ev.HostId,
            ev.HsmSignature.ToByteArray(),
            ev.PublicKey.ToByteArray(),
            ev.ValidFrom.ToDateTime(),
            ev.ValidTo.ToDateTime());

        _publicKeyAggregateDataByKeyId.Add(ev.KeyId, signature);
    }

    private void Apply(EventSignaturePublicKeyDeleted ev)
    {
        var signature = _publicKeyAggregateDataByKeyId.GetValueOrDefault(ev.KeyId)
            ?? throw new ArgumentException($"Cannot process {nameof(EventSignaturePublicKeyDeleted)} for contest {ev.ContestId} because there is no {nameof(EventSignaturePublicKeySigned)} event yet.");

        signature.Deleted = ev.EventInfo.Timestamp.ToDateTime();
    }
}
