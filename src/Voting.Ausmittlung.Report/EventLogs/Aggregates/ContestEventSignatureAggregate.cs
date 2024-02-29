// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Google.Protobuf;
using Voting.Lib.Common;
using AusmittlungEvents = Abraxas.Voting.Ausmittlung.Events.V1;
using BasisEvents = Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class ContestEventSignatureAggregate
{
    private readonly Dictionary<string, EventSignaturePublicKeyAggregateData> _publicKeyAggregateDataByKeyId = new();

    public EventSignaturePublicKeyAggregateData? GetPublicKeyAggregateData(string keyId)
        => _publicKeyAggregateDataByKeyId.GetValueOrDefault(keyId);

    public void Apply(IMessage eventData, IMessage eventMetadata)
    {
        switch ((eventData, eventMetadata))
        {
            case (AusmittlungEvents.EventSignaturePublicKeyCreated data, AusmittlungEvents.Metadata.EventSignaturePublicKeyMetadata metadata):
                Apply(data, metadata);
                break;
            case (AusmittlungEvents.EventSignaturePublicKeyDeleted data, AusmittlungEvents.Metadata.EventSignaturePublicKeyMetadata metadata):
                Apply(data, metadata);
                break;
            case (BasisEvents.EventSignaturePublicKeyCreated data, BasisEvents.Metadata.EventSignaturePublicKeyMetadata metadata):
                Apply(data, metadata);
                break;
            case (BasisEvents.EventSignaturePublicKeyDeleted data, BasisEvents.Metadata.EventSignaturePublicKeyMetadata metadata):
                Apply(data, metadata);
                break;
        }
    }

    private void Apply(AusmittlungEvents.EventSignaturePublicKeyCreated evData, AusmittlungEvents.Metadata.EventSignaturePublicKeyMetadata evMetadata)
    {
        var createData = new EventSignaturePublicKeyAggregateCreateData(
            evData,
            evData.KeyId,
            evData.SignatureVersion,
            GuidParser.Parse(evData.ContestId),
            evData.HostId,
            evData.AuthenticationTag.ToByteArray(),
            evData.PublicKey.ToByteArray(),
            evData.ValidFrom.ToDateTime(),
            evData.ValidTo.ToDateTime(),
            evMetadata.HsmSignature.ToByteArray());

        _publicKeyAggregateDataByKeyId.Add(evData.KeyId, new EventSignaturePublicKeyAggregateData(createData));
    }

    private void Apply(AusmittlungEvents.EventSignaturePublicKeyDeleted evData, AusmittlungEvents.Metadata.EventSignaturePublicKeyMetadata evMetadata)
    {
        var deleteData = new EventSignaturePublicKeyAggregateDeleteData(
            evData,
            evData.KeyId,
            evData.SignatureVersion,
            GuidParser.Parse(evData.ContestId),
            evData.HostId,
            evData.AuthenticationTag.ToByteArray(),
            evData.SignedEventCount,
            evData.DeletedAt.ToDateTime(),
            evMetadata.HsmSignature.ToByteArray());

        var data = _publicKeyAggregateDataByKeyId.GetValueOrDefault(evData.KeyId)
            ?? throw new ArgumentException($"Cannot process {nameof(AusmittlungEvents.EventSignaturePublicKeyDeleted)} for contest {evData.ContestId} because there is no {nameof(AusmittlungEvents.EventSignaturePublicKeyCreated)} event yet.");

        data.DeleteData = deleteData;
    }

    private void Apply(BasisEvents.EventSignaturePublicKeyCreated evData, BasisEvents.Metadata.EventSignaturePublicKeyMetadata evMetadata)
    {
        var createData = new EventSignaturePublicKeyAggregateCreateData(
            evData,
            evData.KeyId,
            evData.SignatureVersion,
            GuidParser.Parse(evData.ContestId),
            evData.HostId,
            evData.AuthenticationTag.ToByteArray(),
            evData.PublicKey.ToByteArray(),
            evData.ValidFrom.ToDateTime(),
            evData.ValidTo.ToDateTime(),
            evMetadata.HsmSignature.ToByteArray());

        _publicKeyAggregateDataByKeyId.Add(evData.KeyId, new EventSignaturePublicKeyAggregateData(createData));
    }

    private void Apply(BasisEvents.EventSignaturePublicKeyDeleted evData, BasisEvents.Metadata.EventSignaturePublicKeyMetadata evMetadata)
    {
        var deleteData = new EventSignaturePublicKeyAggregateDeleteData(
            evData,
            evData.KeyId,
            evData.SignatureVersion,
            GuidParser.Parse(evData.ContestId),
            evData.HostId,
            evData.AuthenticationTag.ToByteArray(),
            evData.SignedEventCount,
            evData.DeletedAt.ToDateTime(),
            evMetadata.HsmSignature.ToByteArray());

        var data = _publicKeyAggregateDataByKeyId.GetValueOrDefault(evData.KeyId)
            ?? throw new ArgumentException($"Cannot process {nameof(AusmittlungEvents.EventSignaturePublicKeyDeleted)} for contest {evData.ContestId} because there is no {nameof(AusmittlungEvents.EventSignaturePublicKeyCreated)} event yet.");

        data.DeleteData = deleteData;
    }
}
