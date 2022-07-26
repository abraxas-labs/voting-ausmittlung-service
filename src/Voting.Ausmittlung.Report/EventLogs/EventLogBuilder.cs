﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task<EventLog?> Build(
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

        var eventInfoProp = EventInfoUtils.GetEventInfoPropertyInfo(ev.Data);
        var eventInfo = EventInfoUtils.MapEventInfo(eventInfoProp.GetValue(ev.Data));
        var eventUser = eventInfo.User;
        var eventTenant = eventInfo.Tenant;
        var eventTimestamp = eventInfo.Timestamp.ToDateTime();

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

        eventLog.Timestamp = eventTimestamp;
        eventLog.EventFullName = ev.Data.Descriptor.FullName;

        await ResolveCountingCircle(eventLog, context);
        ResolvePoliticalBusiness(eventLog, context);

        eventLog.EventUser = new() { Firstname = eventUser.FirstName, Lastname = eventUser.LastName, UserId = eventUser.Id, Username = eventUser.Username };
        eventLog.EventTenant = new() { TenantId = eventTenant.Id, TenantName = eventTenant.Name };

        eventLog.EventSignatureVerification = _eventLogEventSignatureVerifier.VerifyEventSignature(ev, context);

        // Since we extract the event info values, we remove the field, so that the XML doesn't get too huge
        eventInfoProp.SetValue(ev.Data, null);
        eventLog.EventContent = ev.Data.ToByteArray();

        return eventLog;
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

    private void ResolvePoliticalBusiness(EventLog eventLog, EventLogBuilderContext context)
    {
        if (!eventLog.PoliticalBusinessId.HasValue || !eventLog.PoliticalBusinessType.HasValue)
        {
            return;
        }

        var politicalBusinessId = eventLog.PoliticalBusinessId.Value;
        var politicalBusinessType = eventLog.PoliticalBusinessType.Value;

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
            throw new InvalidOperationException($"Political business with id {politicalBusinessId} and type {politicalBusinessType} not found");
        }

        eventLog.Translations = pb.ShortDescription.ToList();
        eventLog.PoliticalBusinessNumber = pb.PoliticalBusinessNumber;
    }
}
