// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.Services.Read;

public class EventLogReader
{
    private readonly MessageConsumerHub<EventProcessedMessage> _eventProcessedHub;
    private readonly IServiceProvider _serviceProvider;
    private readonly ContestRepo _contestRepo;

    public EventLogReader(MessageConsumerHub<EventProcessedMessage> eventProcessedHub, IServiceProvider serviceProvider, ContestRepo contestRepo)
    {
        _eventProcessedHub = eventProcessedHub;
        _serviceProvider = serviceProvider;
        _contestRepo = contestRepo;
    }

    public async Task Watch(
        Guid contestId,
        Guid? basisCountingCircleId,
        IReadOnlyCollection<EventFilter> filters,
        Func<string, EventProcessedMessage, Task> listener,
        CancellationToken cancellationToken)
    {
        var permissionAccessor = _serviceProvider.GetRequiredService<PermissionAccessor>();
        var testingPhaseEnded = await _contestRepo.Query()
            .WhereTestingPhaseEnded()
            .AnyAsync(x => x.Id == contestId, cancellationToken: cancellationToken);
        permissionAccessor.SetContextIds(basisCountingCircleId, contestId, testingPhaseEnded);
        await _eventProcessedHub.Listen(
            permissionAccessor.CanRead,
            e => Task.WhenAll(filters.Where(f => f.Filter(e)).Select(f => listener(f.Id, e))),
            cancellationToken);
    }

    public record EventFilter(
        string Id,
        IReadOnlySet<string> EventTypes,
        Guid? PoliticalBusinessId,
        Guid? PoliticalBusinessResultId,
        Guid? PoliticalBusinessUnionId)
    {
        public bool Filter(EventProcessedMessage e)
        {
            return EventTypes.Contains(e.EventType)
                   && (!PoliticalBusinessId.HasValue || e.PoliticalBusinessId == PoliticalBusinessId)
                   && (!PoliticalBusinessResultId.HasValue || e.PoliticalBusinessResultId == PoliticalBusinessResultId)
                   && (!PoliticalBusinessUnionId.HasValue || e.PoliticalBusinessUnionId == PoliticalBusinessUnionId);
        }
    }
}
