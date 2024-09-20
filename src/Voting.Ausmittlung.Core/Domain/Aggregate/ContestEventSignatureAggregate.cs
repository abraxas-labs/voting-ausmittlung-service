// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using AutoMapper;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Utils;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ContestEventSignatureAggregate : BaseEventSourcingAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;

    public ContestEventSignatureAggregate(IMapper mapper, EventInfoProvider eventInfoProvider)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
    }

    public override string AggregateName => "voting-contestEventSignatureAusmittlung";

    public void CreatePublicKey(EventSignaturePublicKeyCreate data)
    {
        var ev = _mapper.Map<EventSignaturePublicKeyCreated>(data);
        var metadata = _mapper.Map<EventSignaturePublicKeyMetadata>(data);
        ev.EventInfo = _eventInfoProvider.NewEventInfo();
        RaiseEvent(ev, metadata);
    }

    public void DeletePublicKey(EventSignaturePublicKeyDelete data)
    {
        var ev = _mapper.Map<EventSignaturePublicKeyDeleted>(data);
        var metadata = _mapper.Map<EventSignaturePublicKeyMetadata>(data);
        ev.EventInfo = _eventInfoProvider.NewEventInfo();
        RaiseEvent(ev, metadata);
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case EventSignaturePublicKeyCreated data:
                Id = GuidParser.Parse(data.ContestId);
                break;
        }
    }
}
