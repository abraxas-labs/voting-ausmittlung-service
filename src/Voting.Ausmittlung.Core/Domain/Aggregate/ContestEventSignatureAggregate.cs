// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using AutoMapper;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ContestEventSignatureAggregate : BaseEventSourcingAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly Dictionary<string, EventSignaturePublicKeySignature> _signatureByKeyId = new();

    public ContestEventSignatureAggregate(IMapper mapper, EventInfoProvider eventInfoProvider)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
    }

    public override string AggregateName => "voting-contestEventSignature";

    public void CreatePublicKey(EventSignaturePublicKeySignature data)
    {
        var ev = _mapper.Map<EventSignaturePublicKeySigned>(data);
        ev.EventInfo = _eventInfoProvider.NewEventInfo();
        RaiseEvent(ev, new EventSignatureMetadata { ContestId = ev.ContestId });
    }

    public void DeletePublicKey(string keyId, string hostId)
    {
        var ev = new EventSignaturePublicKeyDeleted
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ContestId = Id.ToString(),
            KeyId = keyId,
            HostId = hostId,
        };

        RaiseEvent(ev, new EventSignatureMetadata { ContestId = ev.ContestId });
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case EventSignaturePublicKeySigned e:
                Apply(e);
                break;
            case EventSignaturePublicKeyDeleted e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(EventSignaturePublicKeySigned ev)
    {
        var signature = _mapper.Map<EventSignaturePublicKeySignature>(ev);
        _signatureByKeyId.Add(signature.KeyId, signature);
        Id = signature.ContestId;
    }

    private void Apply(EventSignaturePublicKeyDeleted ev)
    {
        _signatureByKeyId.Remove(ev.KeyId);
    }
}
