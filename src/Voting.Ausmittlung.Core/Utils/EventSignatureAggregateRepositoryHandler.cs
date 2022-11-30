// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.EventSignature;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Basis.Core.EventSignature;

public sealed class EventSignatureAggregateRepositoryHandler : IAggregateRepositoryHandler, IDisposable
{
    private readonly EventSignatureService _eventSignatureService;
    private readonly ContestCache _contestCache;
    private readonly EventSignatureConfig _eventSignatureConfig;

    private IDisposable? _writeLock;

    public EventSignatureAggregateRepositoryHandler(EventSignatureService eventSignatureService, ContestCache contestCache, EventSignatureConfig eventSignatureConfig)
    {
        _eventSignatureService = eventSignatureService;
        _contestCache = contestCache;
        _eventSignatureConfig = eventSignatureConfig;
    }

    public Task BeforeSaved<TAggregate>(TAggregate aggregate)
        where TAggregate : BaseEventSourcingAggregate
    {
        if (IsEventSignatureDisabled(aggregate))
        {
            return Task.CompletedTask;
        }

        _writeLock = _contestCache.BatchWrite();
        _eventSignatureService.FillBusinessMetadata(aggregate);
        return Task.CompletedTask;
    }

    public Task AfterSaved<TAggregate>(TAggregate aggregate, IReadOnlyCollection<IDomainEvent> publishedEvents)
        where TAggregate : BaseEventSourcingAggregate
    {
        if (IsEventSignatureDisabled(aggregate))
        {
            return Task.CompletedTask;
        }

        _eventSignatureService.UpdateSignedEventCount(publishedEvents);
        _writeLock?.Dispose();
        _writeLock = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _writeLock?.Dispose();
    }

    private bool IsEventSignatureDisabled<TAggregate>(TAggregate aggregate)
     where TAggregate : BaseEventSourcingAggregate
    {
        return aggregate is ContestEventSignatureAggregate || !_eventSignatureConfig.Enabled;
    }
}
