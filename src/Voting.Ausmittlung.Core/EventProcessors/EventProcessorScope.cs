// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Subscribe;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.EventProcessors;

/// <summary>
/// Scope for processing an event. Wraps the event processing in a DB transaction and tracks last processed event.
/// </summary>
public sealed class EventProcessorScope : IEventProcessorScope, IDisposable
{
    private readonly IDbRepository<DataContext, EventProcessingState> _repo;
    private readonly EventProcessingInMemoryStateHolder _inMemoryStateHolder;
    private readonly DataContext _dbContext;
    private readonly MessageProducerBuffer _messageHubBuffer;
    private readonly ILogger<EventProcessorScope> _logger;
    private IDbContextTransaction? _transaction;

    public EventProcessorScope(
        IDbRepository<DataContext, EventProcessingState> repo,
        DataContext dbContext,
        MessageProducerBuffer messageHubBuffer,
        ILogger<EventProcessorScope> logger,
        EventProcessingInMemoryStateHolder inMemoryStateHolder)
    {
        _repo = repo;
        _dbContext = dbContext;
        _messageHubBuffer = messageHubBuffer;
        _logger = logger;
        _inMemoryStateHolder = inMemoryStateHolder;
    }

    /// <inheritdoc />
    public async Task Begin(Position position, StreamPosition streamPosition)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        await SetLastProcessedPosition(position, streamPosition);
    }

    /// <inheritdoc />
    public async Task Complete(Position position, StreamPosition streamPosition)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("Event processing cannot complete when transaction is null");
        }

        await _transaction.CommitAsync();

        // only update the in-memory state after the transaction is committed successfully.
        _inMemoryStateHolder.SetState(CreateState(position, streamPosition));
        await _messageHubBuffer.TryComplete();
    }

    /// <inheritdoc />
    public async Task<(Position, StreamPosition)?> GetSnapshotPosition()
    {
        var state = await _repo.GetByKey(EventProcessingState.StaticId);
        return state == null
            ? null
            : (new(state.CommitPosition, state.PreparePosition), state.EventNumber);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }

    private static EventProcessingState CreateState(Position position, ulong eventNumber) =>
        new()
        {
            PreparePosition = position.PreparePosition,
            CommitPosition = position.CommitPosition,
            EventNumber = eventNumber,
        };

    private async Task SetLastProcessedPosition(Position position, StreamPosition streamPosition)
    {
        var eventNumber = (ulong)streamPosition;
        var query = _repo.Query().Where(x => x.Id == EventProcessingState.StaticId);

        // If we already have an in-memory state, ensure that the last processed event matches.
        // If not, ensure the new event number is larger than the last processed event number.
        // The database event state event number may not match our in-memory eventnumber if a transaction to the database
        // was committed successfully, but the database could not write it to the persistent disk.
        // This can happen since we use async persistent disk storage.
        query = _inMemoryStateHolder.State != null
            ? query.Where(x => x.EventNumber == _inMemoryStateHolder.State.EventNumber)
            : query.Where(x => x.EventNumber < eventNumber);

        var updatedRows = await query
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.PreparePosition, position.PreparePosition)
                .SetProperty(x => x.CommitPosition, position.CommitPosition)
                .SetProperty(x => x.EventNumber, eventNumber));

        if (updatedRows > 0)
        {
            // We optimize for the most common use case, where an event processing state already exists
            // and the event number is smaller than the current one.
            return;
        }

        if (!await _repo.Query().AnyAsync())
        {
            await _repo.Create(CreateState(position, eventNumber));
            return;
        }

        // If we get here, the event number was out of order.
        var existingEventProcessingState = await _repo.GetByKey(EventProcessingState.StaticId);

        if (_inMemoryStateHolder.State == null)
        {
            _logger.LogCritical(
                "Received event with number {EventNumber} which seems to be out of order in consideration of current snapshot event number {SnapshotEventNumber}",
                eventNumber,
                existingEventProcessingState?.EventNumber);
            throw new InvalidOperationException($"Received event with number {eventNumber} which seems to be out of order in consideration of current snapshot event number {existingEventProcessingState?.EventNumber}.");
        }

        _logger.LogCritical(
            "Received event with number {EventNumber} which is later than the latest event number marked as processed in-memory {InMemoryEventNumber}. This is likely the result of a database restore.",
            eventNumber,
            _inMemoryStateHolder.State.EventNumber);
        throw new InvalidOperationException($"Received event with number {eventNumber} which is later than the latest event number marked as processed in-memory {_inMemoryStateHolder.State.EventNumber}. This is likely the result of a database restore.");
    }
}
