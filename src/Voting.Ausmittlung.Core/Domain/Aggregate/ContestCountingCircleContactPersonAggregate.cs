// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Core.Utils;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ContestCountingCircleContactPersonAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<ContactPerson> _validator;
    private readonly IMapper _mapper;

    public ContestCountingCircleContactPersonAggregate(
        EventInfoProvider eventInfoProvider,
        IValidator<ContactPerson> validator,
        IMapper mapper,
        EventSignatureService eventSignatureService)
        : base(eventSignatureService, mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _validator = validator;
        _mapper = mapper;
    }

    public Guid ContestId { get; private set; }

    public Guid CountingCircleId { get; private set; }

    public ContactPerson ContactPersonDuringEvent { get; private set; } = new();

    public bool ContactPersonSameDuringEventAsAfter { get; private set; }

    public ContactPerson ContactPersonAfterEvent { get; private set; } = new();

    public override string AggregateName => "voting-contestCountingCircleContactPerson";

    public void Create(
        Guid contestId,
        Guid countingCircleId,
        ContactPerson contactPersonDuringEvent,
        bool contactPersonSameDuringEventAsAfter,
        ContactPerson? contactPersonAfterEvent)
    {
        ValidateContactPersons(contactPersonDuringEvent, contactPersonSameDuringEventAsAfter, contactPersonAfterEvent);

        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }

        var ev = new ContestCountingCircleContactPersonCreated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ContestCountingCircleContactPersonId = Id.ToString(),
            ContestId = contestId.ToString(),
            CountingCircleId = countingCircleId.ToString(),
            ContactPersonDuringEvent = _mapper.Map<ContactPersonEventData>(contactPersonDuringEvent),
            ContactPersonSameDuringEventAsAfter = contactPersonSameDuringEventAsAfter,
            ContactPersonAfterEvent = _mapper.Map<ContactPersonEventData>(contactPersonAfterEvent),
        };
        RaiseEvent(ev, new EventSignatureDomainData(contestId));
    }

    public void Update(ContactPerson contactPersonDuringEvent, bool contactPersonSameDuringEventAsAfter, ContactPerson? contactPersonAfterEvent)
    {
        ValidateContactPersons(contactPersonDuringEvent, contactPersonSameDuringEventAsAfter, contactPersonAfterEvent);

        var ev = new ContestCountingCircleContactPersonUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ContestCountingCircleContactPersonId = Id.ToString(),
            ContactPersonDuringEvent = _mapper.Map<ContactPersonEventData>(contactPersonDuringEvent),
            ContactPersonSameDuringEventAsAfter = contactPersonSameDuringEventAsAfter,
            ContactPersonAfterEvent = _mapper.Map<ContactPersonEventData>(contactPersonAfterEvent),
        };
        RaiseEvent(ev, new EventSignatureDomainData(ContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ContestCountingCircleContactPersonCreated e:
                Apply(e);
                break;
            case ContestCountingCircleContactPersonUpdated e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void ValidateContactPersons(ContactPerson contactPersonDuringEvent, bool contactPersonSameDuringEventAsAfter, ContactPerson? contactPersonAfterEvent)
    {
        _validator.ValidateAndThrow(contactPersonDuringEvent);

        if (!contactPersonSameDuringEventAsAfter)
        {
            if (contactPersonAfterEvent == null)
            {
                throw new ValidationException("ContactPersonAfterEvent cannot be null if ContactPersonSameDuringEventAsAfter is false.");
            }

            _validator.ValidateAndThrow(contactPersonAfterEvent);
        }
    }

    private void Apply(ContestCountingCircleContactPersonCreated ev)
    {
        Id = Guid.Parse(ev.ContestCountingCircleContactPersonId);
        ContestId = Guid.Parse(ev.ContestId);
        CountingCircleId = Guid.Parse(ev.CountingCircleId);
        ContactPersonDuringEvent = _mapper.Map<ContactPerson>(ev.ContactPersonDuringEvent);
        ContactPersonSameDuringEventAsAfter = ev.ContactPersonSameDuringEventAsAfter;
        ContactPersonAfterEvent = _mapper.Map<ContactPerson>(ev.ContactPersonAfterEvent);
    }

    private void Apply(ContestCountingCircleContactPersonUpdated ev)
    {
        ContactPersonDuringEvent = _mapper.Map<ContactPerson>(ev.ContactPersonDuringEvent);
        ContactPersonSameDuringEventAsAfter = ev.ContactPersonSameDuringEventAsAfter;
        ContactPersonAfterEvent = _mapper.Map<ContactPerson>(ev.ContactPersonAfterEvent);
    }
}
