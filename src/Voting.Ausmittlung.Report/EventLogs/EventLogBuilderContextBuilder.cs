// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Report.EventLogs.Aggregates;
using Voting.Lib.Eventing.Read;
using BasisEventSignatureBusinessMetadata = Abraxas.Voting.Basis.Events.V1.Metadata.EventSignatureBusinessMetadata;

namespace Voting.Ausmittlung.Report.EventLogs;

public class EventLogBuilderContextBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventReader _eventReader;
    private readonly ILogger<EventLogBuilderContextBuilder> _logger;

    public EventLogBuilderContextBuilder(IServiceProvider serviceProvider, IEventReader eventReader, ILogger<EventLogBuilderContextBuilder> logger)
    {
        _serviceProvider = serviceProvider;
        _eventReader = eventReader;
        _logger = logger;
    }

    public async Task<EventLogBuilderContext> BuildContext(Guid contestId, IEnumerable<Guid> politicalBusinessIds)
    {
        _logger.LogInformation("EventLogs context build for contest {ContestId} started", contestId);

        var (startPosition, testingPhaseEnded, createdTimestamp) = await GetContestDetails(contestId);
        var contestEventSignatureAggregate = await EagerLoadContestEventSignatureAggregate(contestId);

        return new EventLogBuilderContext(
            contestId,
            testingPhaseEnded,
            politicalBusinessIds,
            contestEventSignatureAggregate,
            startPosition,
            createdTimestamp,
            _serviceProvider);
    }

    private async Task<(Position StartPosition, bool TestingPhaseEnded, DateTime Created)> GetContestDetails(Guid contestId)
    {
        var protoContestId = contestId.ToString();

        // can be simplified by reading from the contest stream with https://jira.abraxas-tools.ch/jira/browse/VOTING-1856.
        var createdOrTestingPhaseEndedEvents = await _eventReader
            .ReadEventsFromAll(
                Position.Start,
                [typeof(ContestCreated), typeof(ContestTestingPhaseEnded)],
                r => (r.Data as ContestTestingPhaseEnded)?.ContestId == protoContestId)
            .Where(r => (r.Metadata as BasisEventSignatureBusinessMetadata)?.ContestId == protoContestId)
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
            // loads to simplify the public keys of basis and ausmittlung into the same report aggregate, although they are from different streams.
            var publicKeySignatureBasisEvents = _eventReader
                .ReadEvents(AggregateNames.Build(AggregateNames.ContestEventSignatureBasis, contestId))
                .Select(ev => new { ev.Data, ev.Metadata });

            var publicKeySignatureAusmittlungEvents = _eventReader
                .ReadEvents(AggregateNames.Build(AggregateNames.ContestEventSignatureAusmittlung, contestId))
                .Select(ev => new { ev.Data, ev.Metadata });

            await foreach (var publicKeySignatureEvent in publicKeySignatureBasisEvents)
            {
                if (publicKeySignatureEvent.Metadata == null)
                {
                    throw new InvalidOperationException("Public key signature on basis event metadata is not set.");
                }

                aggregate.Apply(publicKeySignatureEvent.Data, publicKeySignatureEvent.Metadata);
            }

            await foreach (var publicKeySignatureEvent in publicKeySignatureAusmittlungEvents)
            {
                if (publicKeySignatureEvent.Metadata == null)
                {
                    throw new InvalidOperationException("Public key signature on ausmittlung event metadata is not set.");
                }

                aggregate.Apply(publicKeySignatureEvent.Data, publicKeySignatureEvent.Metadata);
            }

            return aggregate;
        }
        catch (StreamNotFoundException e)
        {
            _logger.LogWarning(e, "Stream {StreamName} not found, could not read public key signatures", e.Stream);
            return aggregate;
        }
    }
}
