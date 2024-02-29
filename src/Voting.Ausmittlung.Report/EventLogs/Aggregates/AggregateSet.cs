// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class AggregateSet<TAggregate>
    where TAggregate : BaseEventSourcingAggregate, IReportAggregate, new()
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ILogger<AggregateSet<TAggregate>> _logger;

    public AggregateSet(
        IAggregateRepository aggregateRepository,
        ILogger<AggregateSet<TAggregate>> logger)
    {
        _aggregateRepository = aggregateRepository;
        _logger = logger;
    }

    protected Dictionary<Guid, TAggregate> Aggregates { get; } = new();

    public void Add(TAggregate aggregate)
    {
        Aggregates.Add(aggregate.Id, aggregate);
    }

    public TAggregate? Get(Guid id)
    {
        return Aggregates.GetValueOrDefault(id);
    }

    public async Task<TAggregate> GetOrLoad(Guid id, DateTime endTimestampInclusive)
    {
        if (Aggregates.TryGetValue(id, out var aggregate))
        {
            return aggregate;
        }

        aggregate = await LoadOrCreateAggregate(id, endTimestampInclusive);
        Aggregates.Add(id, aggregate);
        return aggregate;
    }

    public async Task LoadIfNotCachedAlready(IEnumerable<Guid> ids, DateTime endTimestampInclusive)
    {
        foreach (var id in ids)
        {
            _ = await GetOrLoad(id, endTimestampInclusive);
        }
    }

    private async Task<TAggregate> LoadOrCreateAggregate(Guid id, DateTime endTimestampInclusive)
    {
        try
        {
            return await _aggregateRepository.GetSnapshotById<TAggregate>(id, endTimestampInclusive);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Reading EventStore stream snapshot failed of type {AggregateType} with id {Id} until {EndTimestampInclusive}",
                typeof(TAggregate).Name,
                id,
                endTimestampInclusive);

            var aggregate = new TAggregate();
            aggregate.InitWithId(id);
            return aggregate;
        }
    }
}
