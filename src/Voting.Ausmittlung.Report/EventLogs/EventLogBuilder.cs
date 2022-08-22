// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Ausmittlung.EventSignature.Utils;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Common;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Read;
using EventSignatureMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureMetadata;

namespace Voting.Ausmittlung.Report.EventLogs;

public class EventLogBuilder
{
    private readonly EventLogInitializerAdapterRegistry _eventLogInitializerRegistry;
    private readonly IEventSerializer _eventSerializer;
    private readonly ILogger<EventLogBuilder> _logger;
    private readonly IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> _asymmetricAlgorithmAdapter;

    public EventLogBuilder(
        EventLogInitializerAdapterRegistry eventLogInitializerRegistry,
        IEventSerializer eventSerializer,
        ILogger<EventLogBuilder> logger,
        IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> asymmetricAlgorithmAdapter)
    {
        _eventLogInitializerRegistry = eventLogInitializerRegistry;
        _eventSerializer = eventSerializer;
        _logger = logger;
        _asymmetricAlgorithmAdapter = asymmetricAlgorithmAdapter;
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

        var metadata = ev.Metadata as EventSignatureMetadata;
        if (metadata == null)
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

        eventLog.EventSignatureVerification = VerifyEventSignature(ev, context);

        // Since we extract the event info values, we remove the field, so that the XML doesn't get too huge
        eventInfoProp.SetValue(ev.Data, null);
        eventLog.EventContent = ev.Data.ToByteArray();

        return eventLog;
    }

    private EventLogEventSignatureVerification VerifyEventSignature(EventReadResult ev, EventLogBuilderContext context)
    {
        var eventId = ev.Id;
        var streamName = ev.StreamId;
        var eventInfoProp = EventInfoUtils.GetEventInfoPropertyInfo(ev.Data);
        var eventTimestamp = EventInfoUtils.MapEventInfo(eventInfoProp.GetValue(ev.Data)).Timestamp.ToDateTime();
        var eventSignatureMetadata = ev.Metadata as EventSignatureMetadata
            ?? throw new ArgumentException($"{nameof(ev.Metadata)} may not be null");

        if (string.IsNullOrEmpty(eventSignatureMetadata.KeyId))
        {
            return EventLogEventSignatureVerification.NoSignature;
        }

        var publicKeySignatureValidationResult = context.GetPublicKeySignatureValidationResult(eventSignatureMetadata.KeyId);
        if (publicKeySignatureValidationResult == null)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. No matching public key signature found", eventId, streamName);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (!publicKeySignatureValidationResult.IsValid)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The public key signature is not valid", eventId, streamName);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (publicKeySignatureValidationResult.SignatureData.HostId != eventSignatureMetadata.HostId)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The public key host id does not match with the metadata host id", eventId, streamName);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (eventTimestamp < publicKeySignatureValidationResult.KeyData.ValidFrom
            || eventTimestamp > publicKeySignatureValidationResult.KeyData.ValidTo
            || (publicKeySignatureValidationResult.KeyData.Deleted.HasValue && eventTimestamp > publicKeySignatureValidationResult.KeyData.Deleted))
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The event has a key outside of its lifetime attached", eventId, streamName);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        var eventSignaturePayload = new EventSignaturePayload(
            eventSignatureMetadata.SignatureVersion,
            eventId,
            streamName,
            _eventSerializer.Serialize(ev.Data).ToArray(),
            Guid.Parse(eventSignatureMetadata.ContestId),
            eventSignatureMetadata.HostId,
            eventSignatureMetadata.KeyId,
            eventTimestamp);

        if (!_asymmetricAlgorithmAdapter.VerifySignature(eventSignaturePayload.ConvertToBytesToSign(), eventSignatureMetadata.Signature.ToByteArray(), publicKeySignatureValidationResult.KeyData.Key))
        {
            _logger.LogCritical(
                SecurityLogging.SecurityEventId,
                "Event signature verification for {EventId} in stream {StreamName} failed. The event content does not match the signature",
                eventId,
                streamName);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        return EventLogEventSignatureVerification.VerificationSuccess;
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
