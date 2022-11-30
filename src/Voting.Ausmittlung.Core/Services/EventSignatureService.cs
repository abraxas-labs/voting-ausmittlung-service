// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Ausmittlung.EventSignature.Utils;
using Voting.Lib.Common;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using ProtoEventSignatureBusinessMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureBusinessMetadata;

namespace Voting.Ausmittlung.Core.Services;

/// <summary>
/// A service which handles event signature operations.
/// The methods are not thread-safe.
/// </summary>
public class EventSignatureService
{
    private readonly ILogger<EventSignatureService> _logger;
    private readonly IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> _asymmetricAlgorithmAdapter;
    private readonly IPkcs11DeviceAdapter _pkcs11DeviceAdapter;
    private readonly ContestCache _contestCache;
    private readonly IEventSerializer _eventSerializer;
    private readonly IClock _clock;
    private readonly IServiceProvider _serviceProvider;
    private readonly MachineConfig _machineConfig;
    private readonly IMapper _mapper;

    public EventSignatureService(
        ILogger<EventSignatureService> logger,
        IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> asymmetricAlgorithmAdapter,
        IPkcs11DeviceAdapter pkcs11DeviceAdapter,
        ContestCache contestCache,
        IEventSerializer eventSerializer,
        IClock clock,
        IServiceProvider serviceProvider,
        MachineConfig machineConfig,
        IMapper mapper)
    {
        _logger = logger;
        _asymmetricAlgorithmAdapter = asymmetricAlgorithmAdapter;
        _pkcs11DeviceAdapter = pkcs11DeviceAdapter;
        _contestCache = contestCache;
        _eventSerializer = eventSerializer;
        _clock = clock;
        _serviceProvider = serviceProvider;
        _machineConfig = machineConfig;
        _mapper = mapper;
    }

    /// <summary>
    /// Builds the event signature metadata.
    /// If no signing key is provided for the contest, an event signature metadata object is set only with a contest id.
    /// </summary>
    /// <param name="streamName">Aggregate stream name.</param>
    /// <param name="eventData">Event data.</param>
    /// <param name="contestId">Contest id.</param>
    /// <param name="eventId">Event id.</param>
    /// <returns>An event signature metadata object.</returns>
    public EventSignatureBusinessMetadata BuildBusinessMetadata(string streamName, IMessage eventData, Guid contestId, Guid eventId)
    {
        var keyData = _contestCache.Get(contestId).KeyData;

        // the key data could be null, when a contest is not active or past unlocked.
        if (keyData == null)
        {
            return new EventSignatureBusinessMetadata(contestId);
        }

        return CreateBusinessMetadata(
            eventId,
            eventData,
            keyData,
            streamName,
            contestId);
    }

    /// <summary>
    /// Starts event signature for the current contest. Creates a signed public key for the current host and contest.
    /// </summary>
    /// <param name="contestId">Contest id.</param>
    /// <param name="validTo">End of public key validity.</param>
    /// <returns>A Task.</returns>
    public async Task<ContestCacheEntryKeyData> StartSignature(Guid contestId, DateTime validTo)
    {
        EcdsaPrivateKey? key = null;

        try
        {
            key = _asymmetricAlgorithmAdapter.CreateRandomPrivateKey();

            var authTagPayload = new PublicKeySignatureCreateAuthenticationTagPayload(
                EventSignatureVersions.V1,
                contestId,
                _machineConfig.Name,
                key.Id,
                key.PublicKey,
                _clock.UtcNow,
                validTo);

            var hsmPayload = new PublicKeySignatureCreateHsmPayload(
                authTagPayload,
                _asymmetricAlgorithmAdapter.CreateSignature(authTagPayload.ConvertToBytesToSign(), key));

            var publicKeyCreate = BuildPublicKeyCreate(hsmPayload);

            using var scope = _serviceProvider.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<EventSignatureWriter>();
            await writer.CreatePublicKey(publicKeyCreate);
            var keyData = new ContestCacheEntryKeyData(key, hsmPayload.ValidFrom, hsmPayload.ValidTo);

            _logger.LogInformation(
                SecurityLogging.SecurityEventId,
                "Created signature key pair {KeyId} for contest {ContestId} ({ValidFrom} - {ValidTo})",
                key.Id,
                contestId,
                keyData.ValidFrom,
                keyData.ValidTo);

            return keyData;
        }
        catch (Exception ex)
        {
            key?.Dispose();
            _logger.LogError(SecurityLogging.SecurityEventId, ex, "Start signature for contest {ContestId} failed", contestId);
            throw;
        }
    }

    /// <summary>
    /// Stops event signature for the contest. Deletes the signed public key of the current host and contest.
    /// </summary>
    /// <param name="contestId">Contest id.</param>
    /// <param name="keyId">Event signature key id.</param>
    /// <returns>A Task.</returns>
    /// <exception cref="ArgumentException">If no key is assigned.</exception>
    public async Task StopSignature(Guid contestId, string keyId)
    {
        try
        {
            var keyData = _contestCache.Get(contestId).KeyData;
            if (keyData == null)
            {
                throw new InvalidOperationException("Cannot stop signature, because the key is not set.");
            }

            var authTagPayload = new PublicKeySignatureDeleteAuthenticationTagPayload(
                EventSignatureVersions.V1,
                contestId,
                _machineConfig.Name,
                keyData.Key.Id,
                _clock.UtcNow,
                keyData.SignedEventCount);

            var hsmPayload = new PublicKeySignatureDeleteHsmPayload(
                authTagPayload,
                _asymmetricAlgorithmAdapter.CreateSignature(authTagPayload.ConvertToBytesToSign(), keyData.Key));

            var publicKeyDelete = BuildPublicKeyDelete(hsmPayload);
            using var scope = _serviceProvider.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<EventSignatureWriter>();

            await writer.DeletePublicKey(publicKeyDelete);
            _logger.LogInformation(SecurityLogging.SecurityEventId, "Removed signature key {KeyId} for contest {ContestId}", keyId, contestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(SecurityLogging.SecurityEventId, ex, "Delete public key for contest {ContestId} and key {KeyId} failed", contestId, keyId);
        }
    }

    internal void FillBusinessMetadata<TAggregate>(TAggregate aggregate)
        where TAggregate : BaseEventSourcingAggregate
    {
        var streamName = aggregate.StreamName;

        foreach (var ev in aggregate.GetUncommittedEvents())
        {
            // only contest related events should have their metadata filled, except the public key events.
            // all contest related events except the public key events are assigned only metadata with the contest id.
            if (ev.Metadata is ProtoEventSignatureBusinessMetadata businessMetadata)
            {
                FillBusinessMetadata(businessMetadata, streamName, ev.Data, ev.Id);
            }
        }
    }

    internal void UpdateSignedEventCount(IReadOnlyCollection<IDomainEvent> publishedEvents)
    {
        foreach (var ev in publishedEvents)
        {
            // the count should only be incremented for contest related events, except the public key events.
            if (ev.Metadata is not ProtoEventSignatureBusinessMetadata businessMetadata)
            {
                continue;
            }

            var contestId = GuidParser.Parse(businessMetadata.ContestId);
            var keyData = _contestCache.Get(contestId).KeyData;

            // the key data could be null, when a contest is not active or past unlocked.
            if (keyData == null)
            {
                continue;
            }

            keyData.IncrementSignedEventCount();
        }
    }

    internal EventSignaturePublicKeyCreate BuildPublicKeyCreate(PublicKeySignatureCreateHsmPayload hsmPayload)
    {
        var hsmSignature = _pkcs11DeviceAdapter.CreateSignature(hsmPayload.ConvertToBytesToSign());
        return new EventSignaturePublicKeyCreate
        {
            SignatureVersion = hsmPayload.SignatureVersion,
            ContestId = hsmPayload.ContestId,
            HostId = hsmPayload.HostId,
            KeyId = hsmPayload.KeyId,
            PublicKey = hsmPayload.PublicKey,
            ValidFrom = hsmPayload.ValidFrom,
            ValidTo = hsmPayload.ValidTo,
            AuthenticationTag = hsmPayload.AuthenticationTag,
            HsmSignature = hsmSignature,
        };
    }

    internal EventSignaturePublicKeyDelete BuildPublicKeyDelete(PublicKeySignatureDeleteHsmPayload hsmPayload)
    {
        var hsmSignature = _pkcs11DeviceAdapter.CreateSignature(hsmPayload.ConvertToBytesToSign());
        return new EventSignaturePublicKeyDelete
        {
            SignatureVersion = hsmPayload.SignatureVersion,
            ContestId = hsmPayload.ContestId,
            HostId = hsmPayload.HostId,
            KeyId = hsmPayload.KeyId,
            DeletedAt = hsmPayload.DeletedAt,
            SignedEventCount = hsmPayload.SignedEventCount,
            AuthenticationTag = hsmPayload.AuthenticationTag,
            HsmSignature = hsmSignature,
        };
    }

    internal byte[] CreateBusinessSignature(EventSignatureBusinessPayload businessPayload, EcdsaPrivateKey key)
    {
        return _asymmetricAlgorithmAdapter.CreateSignature(businessPayload.ConvertToBytesToSign(), key);
    }

    internal EventSignatureBusinessPayload BuildBusinessPayload(
        Guid eventId,
        int signatureVersion,
        string streamName,
        Guid contestId,
        IMessage eventData,
        string machineName,
        string keyId,
        DateTime timestamp)
    {
        return new EventSignatureBusinessPayload(
            signatureVersion,
            eventId,
            streamName,
            _eventSerializer.Serialize(eventData).ToArray(),
            contestId,
            machineName,
            keyId,
            timestamp);
    }

    private EventSignatureBusinessMetadata CreateBusinessMetadata(
        Guid eventId,
        IMessage eventData,
        ContestCacheEntryKeyData keyData,
        string streamName,
        Guid contestId)
    {
        if (keyData.Key.PrivateKey == null || keyData.Key.PrivateKey.Length == 0)
        {
            throw new ArgumentException($"Cannot create event metadata for contest {contestId} without a private key");
        }

        var timestamp = EventInfoUtils.GetEventInfo(eventData).Timestamp.ToDateTime();

        if (timestamp < keyData.ValidFrom || timestamp > keyData.ValidTo)
        {
            throw new ArgumentException($"Cannot create event metadata because the current key {keyData.Key.Id} for contest {contestId} is not valid anymore ({keyData.ValidFrom} - {keyData.ValidTo}).");
        }

        var businessPayload = BuildBusinessPayload(
            eventId,
            EventSignatureVersions.V1,
            streamName,
            contestId,
            eventData,
            _machineConfig.Name,
            keyData.Key.Id,
            timestamp);

        var businessSignature = CreateBusinessSignature(businessPayload, keyData.Key);

        return new EventSignatureBusinessMetadata(
            contestId,
            EventSignatureVersions.V1,
            businessPayload.HostId,
            keyData.Key.Id,
            businessSignature);
    }

    private void FillBusinessMetadata(ProtoEventSignatureBusinessMetadata businessMetadata, string streamName, IMessage eventData, Guid eventId)
    {
        // If metadata is defined, it will only contain a contest id.
        var contestId = GuidParser.Parse(businessMetadata.ContestId);

        var domainEventSignatureBusinessMetadata = BuildBusinessMetadata(streamName, eventData, contestId, eventId);
        _mapper.Map(domainEventSignatureBusinessMetadata, businessMetadata);
    }
}
