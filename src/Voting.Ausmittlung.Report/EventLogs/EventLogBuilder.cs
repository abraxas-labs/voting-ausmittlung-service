// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.EventSignature.Utils;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Eventing.Read;
using AusmittlungEventSignatureBusinessMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureBusinessMetadata;
using BasisEventSignatureBusinessMetadata = Abraxas.Voting.Basis.Events.V1.Metadata.EventSignatureBusinessMetadata;

namespace Voting.Ausmittlung.Report.EventLogs;

public class EventLogBuilder
{
    private readonly EventLogInitializerAdapterRegistry _eventLogInitializerRegistry;
    private readonly ILogger<EventLogBuilder> _logger;
    private readonly EventLogEventSignatureVerifier _eventLogEventSignatureVerifier;

    public EventLogBuilder(
        EventLogInitializerAdapterRegistry eventLogInitializerRegistry,
        ILogger<EventLogBuilder> logger,
        EventLogEventSignatureVerifier eventLogEventSignatureVerifier)
    {
        _eventLogInitializerRegistry = eventLogInitializerRegistry;
        _logger = logger;
        _eventLogEventSignatureVerifier = eventLogEventSignatureVerifier;
    }

    public async Task<EventLog?> BuildBusinessEventLog(
        EventReadResult ev,
        EventLogBuilderContext context)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Id"] = ev.Id,
            ["StreamId"] = ev.StreamId,
            ["CommitPosition"] = ev.Position.CommitPosition,
        });

        if (ev.Metadata is not AusmittlungEventSignatureBusinessMetadata && ev.Metadata is not BasisEventSignatureBusinessMetadata)
        {
            return null;
        }

        IncrementSignedEventCount(ev.Metadata, context);

        if (IsIgnoredEvent(ev))
        {
            return null;
        }

        var initializer = _eventLogInitializerRegistry.GetInitializerAdapter(ev.Data.Descriptor);
        if (initializer == null)
        {
            return null;
        }

        var eventLog = initializer.Process(ev.Data, context);
        if (eventLog == null)
        {
            return null;
        }

        await ResolveCountingCircle(eventLog, context);
        if (!ResolvePoliticalBusiness(eventLog, context))
        {
            return null;
        }

        eventLog.EventSignatureVerification = _eventLogEventSignatureVerifier.VerifyEventSignature(ev, context);

        MapEventDataToEventLog(eventLog, ev.Data);

        return eventLog;
    }

    public virtual EventLog BuildSignatureEventLog(IMessage message)
    {
        var eventLog = new EventLog();
        MapEventDataToEventLog(eventLog, message);
        return eventLog;
    }

    private void MapEventDataToEventLog(EventLog eventLog, IMessage eventData)
    {
        var eventInfoProp = EventInfoUtils.GetEventInfoPropertyInfo(eventData);
        var eventInfo = EventInfoUtils.MapEventInfo(eventInfoProp.GetValue(eventData));
        var eventTimestamp = eventInfo.Timestamp.ToDateTime();
        var eventUser = eventInfo.User;
        var eventTenant = eventInfo.Tenant;

        eventLog.Timestamp = eventTimestamp;
        eventLog.EventFullName = eventData.Descriptor.FullName;

        eventLog.EventUser = new() { Firstname = eventUser.FirstName, Lastname = eventUser.LastName, UserId = eventUser.Id, Username = eventUser.Username };
        eventLog.EventTenant = new() { TenantId = eventTenant.Id, TenantName = eventTenant.Name };

        // Since we extract the event info values, we remove the field, so that the export doesn't get too huge
        eventInfoProp.SetValue(eventData, null);
        eventLog.EventContent = eventData;
    }

    private void IncrementSignedEventCount(IMessage eventMetadata, EventLogBuilderContext context)
    {
        var keyId = eventMetadata switch
        {
            AusmittlungEventSignatureBusinessMetadata ausmittlungEventMetadata => ausmittlungEventMetadata.KeyId,
            BasisEventSignatureBusinessMetadata basisEventMetadata => basisEventMetadata.KeyId,
            _ => throw new InvalidOperationException("Invalid event metadata type"),
        };

        if (!string.IsNullOrEmpty(keyId))
        {
            context.IncrementSignedEventCount(keyId);
        }
    }

    private async Task ResolveCountingCircle(EventLog eventLog, EventLogBuilderContext context)
    {
        if (!eventLog.CountingCircleId.HasValue)
        {
            return;
        }

        var aggregate = await context.CountingCircleAggregateSet.GetOrLoad(eventLog.CountingCircleId.Value, context.CurrentTimestampInStream)
            ?? throw new InvalidOperationException($"Counting circle {eventLog.CountingCircleId} not found");

        eventLog.CountingCircle = aggregate.MapToBasisCountingCircle();
    }

    private bool ResolvePoliticalBusiness(EventLog eventLog, EventLogBuilderContext context)
    {
        if (!eventLog.PoliticalBusinessId.HasValue || !eventLog.PoliticalBusinessType.HasValue)
        {
            return true;
        }

        var politicalBusinessId = eventLog.PoliticalBusinessId.Value;
        var politicalBusinessType = eventLog.PoliticalBusinessType.Value;

        if (!context.PoliticalBusinessIdsFilter.Contains(politicalBusinessId) && politicalBusinessType != PoliticalBusinessType.SecondaryMajorityElection)
        {
            return false;
        }

        IPoliticalBusiness? pb = politicalBusinessType switch
        {
            PoliticalBusinessType.Vote =>
                context.VoteAggregateSet.Get(politicalBusinessId),
            PoliticalBusinessType.ProportionalElection =>
                context.ProportionalElectionAggregateSet.Get(politicalBusinessId),
            PoliticalBusinessType.MajorityElection =>
                context.MajorityElectionAggregateSet.Get(politicalBusinessId),
            PoliticalBusinessType.SecondaryMajorityElection =>
                context.MajorityElectionAggregateSet.GetBySecondaryMajorityElectionId(politicalBusinessId)?
                    .GetSecondaryMajorityElection(politicalBusinessId),
            _ => throw new InvalidOperationException("Invalid political business type"),
        };

        if (pb == null)
        {
            return false;
        }

        eventLog.Translations = pb.ShortDescription.ToList();
        eventLog.PoliticalBusinessNumber = pb.PoliticalBusinessNumber;

        return true;
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
}
