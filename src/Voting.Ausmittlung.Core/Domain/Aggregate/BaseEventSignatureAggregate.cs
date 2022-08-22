// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using AutoMapper;
using EventStore.Client;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Services;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public abstract class BaseEventSignatureAggregate : BaseEventSourcingAggregate
{
    private readonly EventSignatureService _eventSignatureService;
    private readonly IMapper _mapper;

    protected BaseEventSignatureAggregate(EventSignatureService eventSignatureService, IMapper mapper)
    {
        _eventSignatureService = eventSignatureService;
        _mapper = mapper;
    }

    /// <summary>
    /// Raises an event and applies an event signature by the provided eventDataToSign if necessary.
    /// This stores the event in the uncommited events and immediately applies it to this aggregate.
    /// </summary>
    /// <param name="eventData">The event data of the event.</param>
    /// <param name="eventSignatureDomainData">The domain data to provide metadata informations.</param>
    protected void RaiseEvent(IMessage eventData, EventSignatureDomainData eventSignatureDomainData)
    {
        var eventId = Uuid.NewUuid().ToGuid();
        var metadata = _eventSignatureService.BuildEventSignatureMetadata(StreamName, eventData, eventSignatureDomainData.ContestId, eventId);
        RaiseEvent(
            eventData,
            _mapper.Map<EventSignatureMetadata>(metadata),
            eventId);
    }
}
