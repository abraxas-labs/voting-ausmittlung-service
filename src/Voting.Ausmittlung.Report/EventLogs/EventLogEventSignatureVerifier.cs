// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Ausmittlung.EventSignature.Utils;
using Voting.Lib.Common;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Read;
using AusmittlungEventSignatureBusinessMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureBusinessMetadata;
using BasisEventSignatureBusinessMetadata = Abraxas.Voting.Basis.Events.V1.Metadata.EventSignatureBusinessMetadata;

namespace Voting.Ausmittlung.Report.EventLogs;

public class EventLogEventSignatureVerifier
{
    private readonly IEventSerializer _eventSerializer;
    private readonly ILogger<EventLogEventSignatureVerifier> _logger;
    private readonly IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> _asymmetricAlgorithmAdapter;

    public EventLogEventSignatureVerifier(
        IEventSerializer eventSerializer,
        ILogger<EventLogEventSignatureVerifier> logger,
        IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> asymmetricAlgorithmAdapter)
    {
        _eventSerializer = eventSerializer;
        _logger = logger;
        _asymmetricAlgorithmAdapter = asymmetricAlgorithmAdapter;
    }

    public EventLogEventSignatureVerification VerifyEventSignature(EventReadResult ev, EventLogBuilderContext context)
    {
        if (ev.Metadata is AusmittlungEventSignatureBusinessMetadata ausmittlungEventSignatureBusiessMetadata)
        {
            return VerifyAusmittlungEventSignature(ev, context, ausmittlungEventSignatureBusiessMetadata);
        }

        if (ev.Metadata is BasisEventSignatureBusinessMetadata basisEventSignatureBusiessMetadata)
        {
            return VerifyBasisEventSignature(ev, context, basisEventSignatureBusiessMetadata);
        }

        throw new ArgumentException($"{nameof(ev.Metadata)} may not be null");
    }

    private EventLogEventSignatureVerification VerifyBasisEventSignature(
        EventReadResult ev,
        EventLogBuilderContext context,
        BasisEventSignatureBusinessMetadata eventSignatureBusinessMetadata)
    {
        var eventInfoProp = EventInfoUtils.GetEventInfoPropertyInfo(ev.Data);
        var eventTimestamp = EventInfoUtils.MapEventInfo(eventInfoProp.GetValue(ev.Data)).Timestamp.ToDateTime();

        if (string.IsNullOrEmpty(eventSignatureBusinessMetadata.KeyId))
        {
            return EventLogEventSignatureVerification.NoSignature;
        }

        var publicKeySignatureValidationResult = context.GetPublicKeySignatureValidationResult(eventSignatureBusinessMetadata.KeyId);
        if (publicKeySignatureValidationResult == null)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. No matching public key signature found", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (!publicKeySignatureValidationResult.IsValid)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The public key signature is not valid", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (publicKeySignatureValidationResult.SignatureData!.HostId != eventSignatureBusinessMetadata.HostId)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The public key host id does not match with the metadata host id", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (publicKeySignatureValidationResult.SignatureData.SignatureVersion != eventSignatureBusinessMetadata.SignatureVersion)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The public key signature version does not match with the metadata signature version", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (eventTimestamp < publicKeySignatureValidationResult.KeyData.ValidFrom
            || eventTimestamp > publicKeySignatureValidationResult.KeyData.ValidTo
            || (publicKeySignatureValidationResult.KeyData.DeletedAt.HasValue && eventTimestamp > publicKeySignatureValidationResult.KeyData.DeletedAt))
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The event has a key outside of its lifetime attached", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        var eventSignatureBusinessPayload = new EventSignatureBusinessPayload(
            eventSignatureBusinessMetadata.SignatureVersion,
            ev.Id,
            ev.StreamId,
            _eventSerializer.Serialize(ev.Data).ToArray(),
            GuidParser.Parse(eventSignatureBusinessMetadata.ContestId),
            eventSignatureBusinessMetadata.HostId,
            eventSignatureBusinessMetadata.KeyId,
            eventTimestamp);

        if (!_asymmetricAlgorithmAdapter.VerifySignature(eventSignatureBusinessPayload.ConvertToBytesToSign(), eventSignatureBusinessMetadata.Signature.ToByteArray(), publicKeySignatureValidationResult.KeyData.Key))
        {
            _logger.LogCritical(
                SecurityLogging.SecurityEventId,
                "Event signature verification for {EventId} in stream {StreamName} failed. The event content does not match the signature",
                ev.Id,
                ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        return EventLogEventSignatureVerification.VerificationSuccess;
    }

    private EventLogEventSignatureVerification VerifyAusmittlungEventSignature(
        EventReadResult ev,
        EventLogBuilderContext context,
        AusmittlungEventSignatureBusinessMetadata eventSignatureBusinessMetadata)
    {
        var eventInfoProp = EventInfoUtils.GetEventInfoPropertyInfo(ev.Data);
        var eventTimestamp = EventInfoUtils.MapEventInfo(eventInfoProp.GetValue(ev.Data)).Timestamp.ToDateTime();

        if (string.IsNullOrEmpty(eventSignatureBusinessMetadata.KeyId))
        {
            return EventLogEventSignatureVerification.NoSignature;
        }

        var publicKeySignatureValidationResult = context.GetPublicKeySignatureValidationResult(eventSignatureBusinessMetadata.KeyId);
        if (publicKeySignatureValidationResult == null)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. No matching public key signature found", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (!publicKeySignatureValidationResult.IsValid)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The public key signature is not valid", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (publicKeySignatureValidationResult.SignatureData!.HostId != eventSignatureBusinessMetadata.HostId)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The public key host id does not match with the metadata host id", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (publicKeySignatureValidationResult.SignatureData.SignatureVersion != eventSignatureBusinessMetadata.SignatureVersion)
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The public key signature version does not match with the metadata signature version", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        if (eventTimestamp < publicKeySignatureValidationResult.KeyData.ValidFrom
            || eventTimestamp > publicKeySignatureValidationResult.KeyData.ValidTo
            || (publicKeySignatureValidationResult.KeyData.DeletedAt.HasValue && eventTimestamp > publicKeySignatureValidationResult.KeyData.DeletedAt))
        {
            _logger.LogCritical(SecurityLogging.SecurityEventId, "Event signature verification for {EventId} in stream {StreamName} failed. The event has a key outside of its lifetime attached", ev.Id, ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        var eventSignatureBusinessPayload = new EventSignatureBusinessPayload(
            eventSignatureBusinessMetadata.SignatureVersion,
            ev.Id,
            ev.StreamId,
            _eventSerializer.Serialize(ev.Data).ToArray(),
            GuidParser.Parse(eventSignatureBusinessMetadata.ContestId),
            eventSignatureBusinessMetadata.HostId,
            eventSignatureBusinessMetadata.KeyId,
            eventTimestamp);

        if (!_asymmetricAlgorithmAdapter.VerifySignature(eventSignatureBusinessPayload.ConvertToBytesToSign(), eventSignatureBusinessMetadata.Signature.ToByteArray(), publicKeySignatureValidationResult.KeyData.Key))
        {
            _logger.LogCritical(
                SecurityLogging.SecurityEventId,
                "Event signature verification for {EventId} in stream {StreamName} failed. The event content does not match the signature",
                ev.Id,
                ev.StreamId);
            return EventLogEventSignatureVerification.VerificationFailed;
        }

        return EventLogEventSignatureVerification.VerificationSuccess;
    }
}
