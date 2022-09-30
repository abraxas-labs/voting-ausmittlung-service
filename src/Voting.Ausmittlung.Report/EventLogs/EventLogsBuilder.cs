// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using Abraxas.Voting.Basis.Events.V1;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs.Aggregates;
using Voting.Lib.Eventing.Read;

namespace Voting.Ausmittlung.Report.EventLogs;

/// <summary>
/// Builds a log of all events that happened during a "live" contest (after the testing phase ended).
/// Also verifies the event signatures.
/// </summary>
public class EventLogsBuilder
{
    private readonly IEventReader _eventReader;
    private readonly ILogger<EventLogsBuilder> _logger;
    private readonly EventLogBuilder _eventLogBuilder;
    private readonly IServiceProvider _serviceProvider;

    public EventLogsBuilder(
        IEventReader eventReader,
        ILogger<EventLogsBuilder> logger,
        EventLogBuilder eventLogBuilder,
        IServiceProvider serviceProvider)
    {
        _eventReader = eventReader;
        _logger = logger;
        _eventLogBuilder = eventLogBuilder;
        _serviceProvider = serviceProvider;
    }

    public async IAsyncEnumerable<EventLog> Build(Contest contest, IEnumerable<Guid> politicalBusinessIds)
    {
        _logger.LogInformation("EventLogs build for contest {ContestId} started", contest.Id);
        var (startPosition, testingPhaseEnded, createdTimestamp) = await GetContestDetails(contest.Id);
        var contestEventSignatureAggregate = testingPhaseEnded
            ? await EagerLoadContestEventSignatureAggregate(contest.Id)
            : new();

        var events = _eventReader
            .ReadEventsFromAll(
                startPosition,
                ev => (ev.Data as ContestArchived)?.ContestId == contest.Id.ToString(),
                EventSignatureMetadata.Descriptor)
            .Where(ev => IsInContestOrRelevated(ev, contest.Id.ToString()));

        using var context = new EventLogBuilderContext(
            contest.Id,
            testingPhaseEnded,
            politicalBusinessIds,
            contestEventSignatureAggregate,
            startPosition,
            createdTimestamp,
            _serviceProvider);

        if (context.TestingPhaseEnded)
        {
            await EagerLoadPoliticalBusinessAggregates(contest, context);
        }

        await foreach (var ev in events)
        {
            context.CurrentTimestampInStream = ev.Created;

            var eventLog = await _eventLogBuilder.Build(ev, context);

            if (eventLog == null)
            {
                continue;
            }

            yield return eventLog;
        }

        _logger.LogInformation("EventLogs build for contest {ContestId} completed", contest.Id);
    }

    private async Task<(Position StartPosition, bool TestingPhaseEnded, DateTime Created)> GetContestDetails(Guid contestId)
    {
        var protoContestId = contestId.ToString();

        // can be simplified by reading from the contest stream with https://jira.abraxas-tools.ch/jira/browse/VOTING-1856.
        var createdOrTestingPhaseEndedEvents = await _eventReader
            .ReadEventsFromAll(
                Position.Start,
                new[] { typeof(ContestCreated), typeof(ContestTestingPhaseEnded) },
                r => (r.Data as ContestTestingPhaseEnded)?.ContestId == protoContestId,
                EventSignatureMetadata.Descriptor)
            .Where(r => (r.Metadata as EventSignatureMetadata)?.ContestId == protoContestId)
            .ToListAsync();

        // the significant event is the ContestTestingPhaseEnded if present, the ContestCreated otherwise (if in testing phase).
        return createdOrTestingPhaseEndedEvents.Count switch
        {
            1 => (createdOrTestingPhaseEndedEvents[0].Position, false, createdOrTestingPhaseEndedEvents[0].Created),
            2 => (createdOrTestingPhaseEndedEvents[1].Position, true, createdOrTestingPhaseEndedEvents[1].Created),
            _ => throw new ArgumentException("Could not determine the event log event read start position"),
        };
    }

    private async Task<ContestEventSignatureAggregate> EagerLoadContestEventSignatureAggregate(Guid contestId)
    {
        var aggregate = new ContestEventSignatureAggregate();
        try
        {
            var publicKeySignatureEvents = _eventReader
                .ReadEvents(AggregateNames.Build(AggregateNames.ContestEventSignature, contestId))
                .Select(ev => ev.Data);

            await foreach (var publicKeySignatureEvent in publicKeySignatureEvents)
            {
                aggregate.Apply(publicKeySignatureEvent);
            }

            return aggregate;
        }
        catch (StreamNotFoundException e)
        {
            _logger.LogWarning(e, "Stream {StreamName} not found, could not read public key signatures", e.Stream);
            return aggregate;
        }
    }

    private bool IsInContestOrRelevated(EventReadResult ev, string contestId)
    {
        if (IsIgnoredEvent(ev))
        {
            return false;
        }

        return (ev.Metadata as EventSignatureMetadata)?.ContestId == contestId
            || ev.Data
                is CountingCircleCreated
                or CountingCircleUpdated
                or CountingCirclesMergerScheduled
                or CountingCirclesMergerScheduled
                or CountingCircleDeleted
                or CountingCirclesMergerScheduleDeleted;
    }

    private bool IsIgnoredEvent(EventReadResult ev)
    {
        // some events should be ignored (eg. exports).
        return ev.Data
            is ResultExportGenerated
            or ResultExportTriggered
            or ResultExportCompleted
            or ExportGenerated
            or BundleReviewExportGenerated;
    }

    // political business aggregates are eager loaded after testing phase to simplify the aggregate resolving.
    // ex: resolve a majority election aggregate when the first incoming event after testing phase ended is a
    // secondary majority election event.
    // this is tamper proof because the pb ids to resolve are coming from the events
    // and the resolver does not lazy load a pb and throws an exception when a pb is not found.
    private Task EagerLoadPoliticalBusinessAggregates(Contest contest, EventLogBuilderContext context)
    {
        var startTimestampInStream = context.StartTimestampInStream;

        // after testing phase a pb must exist in the read model or it throws.
        var pbReadModelIds = contest.SimplePoliticalBusinesses.Select(pb => pb.Id).ToList();
        var pbIdNotInReadModel = context.PoliticalBusinessIdsFilter.FirstOrDefault(pbId => !pbReadModelIds.Contains(pbId));

        if (pbIdNotInReadModel != Guid.Empty)
        {
            throw new ArgumentException($"Attempt to build EventLog for political business {pbIdNotInReadModel}, but not found in read model");
        }

        var idsByPbType = contest.SimplePoliticalBusinesses
            .Where(pb => context.PoliticalBusinessIdsFilter.Contains(pb.Id))
            .GroupBy(pb => pb.PoliticalBusinessType)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<Guid>)x.Select(y => y.Id).ToList());

        return EagerLoadPoliticalBusinessAggregates(context, startTimestampInStream, idsByPbType);
    }

    private async Task EagerLoadPoliticalBusinessAggregates(
        EventLogBuilderContext context,
        DateTime startTimestampInStream,
        IReadOnlyDictionary<PoliticalBusinessType, IReadOnlyCollection<Guid>> idsByPbType)
    {
        foreach (var (pbType, pbIds) in idsByPbType)
        {
            switch (pbType)
            {
                case PoliticalBusinessType.Vote:
                    await context.VoteAggregateSet.LoadIfNotCachedAlready(pbIds, startTimestampInStream);
                    break;
                case PoliticalBusinessType.ProportionalElection:
                    await context.ProportionalElectionAggregateSet.LoadIfNotCachedAlready(pbIds, startTimestampInStream);
                    break;
                case PoliticalBusinessType.MajorityElection:
                    await context.MajorityElectionAggregateSet.LoadIfNotCachedAlready(pbIds, startTimestampInStream);
                    break;
            }
        }
    }
}
