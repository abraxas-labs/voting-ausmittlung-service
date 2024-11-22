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
    private readonly DataContext _dbContext;
    private readonly MessageProducerBuffer _messageHubBuffer;
    private readonly ILogger<EventProcessorScope> _logger;
    private IDbContextTransaction? _transaction;

    public EventProcessorScope(
        IDbRepository<DataContext, EventProcessingState> repo,
        DataContext dbContext,
        MessageProducerBuffer messageHubBuffer,
        ILogger<EventProcessorScope> logger)
    {
        _repo = repo;
        _dbContext = dbContext;
        _messageHubBuffer = messageHubBuffer;
        _logger = logger;
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

    private async Task SetLastProcessedPosition(Position position, StreamPosition streamPosition)
    {
        var eventNumber = (ulong)streamPosition;
        var updatedRows = await _repo.Query()
            .Where(x => x.Id == EventProcessingState.StaticId && x.EventNumber < eventNumber)
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
            await _repo.Create(new EventProcessingState
            {
                PreparePosition = position.PreparePosition,
                CommitPosition = position.CommitPosition,
                EventNumber = eventNumber,
            });
            return;
        }

        // If we get here, the event number was out of order.
        var existingEventProcessingState = await _repo.GetByKey(EventProcessingState.StaticId);
        _logger.LogCritical(
            "Received event with number {EventNumber} which seems to be out of order in consideration of current snapshot event number {SnapshotEventNumber}",
            eventNumber,
            existingEventProcessingState!.EventNumber);
        throw new InvalidOperationException($"Received event {eventNumber} out of order");
    }
}
