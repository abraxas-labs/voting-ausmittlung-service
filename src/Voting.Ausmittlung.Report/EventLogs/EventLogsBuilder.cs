// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using Abraxas.Voting.Basis.Events.V1;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Read;
using BasisEventSignatureBusinessMetadata = Abraxas.Voting.Basis.Events.V1.Metadata.EventSignatureBusinessMetadata;

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

    public EventLogsBuilder(
        IEventReader eventReader,
        ILogger<EventLogsBuilder> logger,
        EventLogBuilder eventLogBuilder)
    {
        _eventReader = eventReader;
        _logger = logger;
        _eventLogBuilder = eventLogBuilder;
    }

    public async IAsyncEnumerable<EventLog> BuildBusinessEventLogs(Contest contest, EventLogBuilderContext context)
    {
        _logger.LogInformation("EventLogs build for contest {ContestId} started", contest.Id);

        var events = _eventReader
            .ReadEventsFromAll(
                context.StartPosition,
                ev => (ev.Data as ContestArchived)?.ContestId == contest.Id.ToString(),
                data => AppDescriptorProvider.GetBusinessMetadataDescriptor(data))
            .Where(ev => IsInContestOrRelated(ev, contest.Id.ToString()));

        if (context.TestingPhaseEnded)
        {
            await EagerLoadPoliticalBusinessAggregates(contest, context);
        }

        await foreach (var ev in events)
        {
            context.CurrentTimestampInStream = ev.Created;

            var eventLog = await _eventLogBuilder.BuildBusinessEventLog(ev, context);

            if (eventLog == null)
            {
                continue;
            }

            yield return eventLog;
        }

        _logger.LogInformation("EventLogs build for contest {ContestId} completed", contest.Id);
    }

    public IReadOnlyCollection<EventLog> BuildPublicKeySignatureEventLogs(EventLogBuilderContext context)
    {
        var events = new List<EventLog>();

        // only add event logs for keys which were used in the activity protocol to verify at least one business event signature.
        foreach (var publicKeyValidationResult in context.GetPublicKeySignatureValidationResults())
        {
            var aggregateData = context.GetPublicKeyAggregateData(publicKeyValidationResult.KeyData.Key.Id)
                ?? throw new InvalidOperationException("Aggregate data must not be null");

            var createPublicKeySignatureEventLog = _eventLogBuilder.BuildSignatureEventLog(aggregateData.CreateData.EventData);
            createPublicKeySignatureEventLog.PublicKeyData = new()
            {
                SignatureValidationResultType = publicKeyValidationResult.CreatePublicKeySignatureValidationResultType,
            };

            events.Add(createPublicKeySignatureEventLog);

            if (aggregateData.DeleteData == null || publicKeyValidationResult.DeletePublicKeySignatureValidationResultType == null)
            {
                continue;
            }

            var deletePublicKeySignatureEventLog = _eventLogBuilder.BuildSignatureEventLog(aggregateData.DeleteData.EventData);
            deletePublicKeySignatureEventLog.PublicKeyData = new()
            {
                SignatureValidationResultType = publicKeyValidationResult.DeletePublicKeySignatureValidationResultType.Value,
                ExpectedSignedEventCount = aggregateData.DeleteData.SignedEventCount,
                ReadAtGenerationSignedEventCount = context.GetReadAtGenerationSignedEventCount(publicKeyValidationResult.KeyData.Key.Id),
            };

            if (deletePublicKeySignatureEventLog.PublicKeyData.HasMatchingSignedEventCount == false)
            {
                _logger.LogWarning(
                    SecurityLogging.SecurityEventId,
                    "Key {KeyId} has a mismatch of the signed event count. Read at generation: {ReadAtGenerationSignedEventCount}, expected: {ExpectedSignedEventCount}",
                    publicKeyValidationResult.KeyData.Key.Id,
                    deletePublicKeySignatureEventLog.PublicKeyData.ReadAtGenerationSignedEventCount,
                    deletePublicKeySignatureEventLog.PublicKeyData.ExpectedSignedEventCount);
            }

            events.Add(deletePublicKeySignatureEventLog);
        }

        return events.OrderBy(e => e.Timestamp).ToList();
    }

    private bool IsInContestOrRelated(EventReadResult ev, string contestId)
    {
        return (ev.Metadata as BasisEventSignatureBusinessMetadata)?.ContestId == contestId
            || (ev.Metadata as EventSignatureBusinessMetadata)?.ContestId == contestId
            || ev.Data
                is CountingCircleCreated
                or CountingCircleUpdated
                or CountingCirclesMergerScheduled
                or CountingCirclesMergerScheduled
                or CountingCircleDeleted
                or CountingCirclesMergerScheduleDeleted;
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
