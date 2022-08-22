// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
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
using Voting.Lib.Eventing.Persistence;

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
    private readonly IContestKeyDataProvider _contestKeyDataProvider;
    private readonly IEventSerializer _eventSerializer;
    private readonly IClock _clock;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventSignatureConfig _eventSignatureConfig;
    private readonly MachineConfig _machineConfig;

    public EventSignatureService(
        ILogger<EventSignatureService> logger,
        IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> asymmetricAlgorithmAdapter,
        IPkcs11DeviceAdapter pkcs11DeviceAdapter,
        IContestKeyDataProvider contestKeyDataProvider,
        IEventSerializer eventSerializer,
        IClock clock,
        IServiceProvider serviceProvider,
        EventSignatureConfig eventSignatureConfig,
        MachineConfig machineConfig)
    {
        _logger = logger;
        _asymmetricAlgorithmAdapter = asymmetricAlgorithmAdapter;
        _pkcs11DeviceAdapter = pkcs11DeviceAdapter;
        _contestKeyDataProvider = contestKeyDataProvider;
        _eventSerializer = eventSerializer;
        _clock = clock;
        _serviceProvider = serviceProvider;
        _eventSignatureConfig = eventSignatureConfig;
        _machineConfig = machineConfig;
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
    public EventSignatureMetadata BuildEventSignatureMetadata(string streamName, IMessage eventData, Guid contestId, Guid eventId)
    {
        if (!_eventSignatureConfig.Enabled)
        {
            return new EventSignatureMetadata(contestId);
        }

        return _contestKeyDataProvider.WithKeyData(contestId, keyData =>
        {
            if (keyData == null)
            {
                return new EventSignatureMetadata(contestId);
            }

            return CreateEventSignatureMetadata(
                eventId,
                eventData,
                keyData,
                streamName,
                contestId);
        });
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

            var hsmPayload = new PublicKeySignaturePayload(
                EventSignatureVersions.V1,
                contestId,
                _machineConfig.Name,
                key.Id,
                key.PublicKey,
                _clock.UtcNow,
                validTo);

            var publicKeySignature = CreatePublicKeySignature(hsmPayload);

            using var scope = _serviceProvider.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<EventSignatureWriter>();
            await writer.CreatePublicKey(publicKeySignature);
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
            _logger.LogError(ex, "Start signature for contest {ContestId} failed", contestId);
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
            using var scope = _serviceProvider.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<EventSignatureWriter>();

            await writer.DeletePublicKey(contestId, keyId, _machineConfig.Name);
            _logger.LogInformation(SecurityLogging.SecurityEventId, "Removed signature key {KeyId} for contest {ContestId}", keyId, contestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(SecurityLogging.SecurityEventId, ex, "Delete public key for contest {ContestId} and key {KeyId} failed", contestId, keyId);
        }
    }

    internal EventSignaturePublicKeySignature CreatePublicKeySignature(PublicKeySignaturePayload hsmPayload)
    {
        var hsmSignature = _pkcs11DeviceAdapter.CreateSignature(hsmPayload.ConvertToBytesToSign());
        return new EventSignaturePublicKeySignature
        {
            SignatureVersion = hsmPayload.SignatureVersion,
            ContestId = hsmPayload.ContestId,
            HostId = hsmPayload.HostId,
            KeyId = hsmPayload.KeyId,
            PublicKey = hsmPayload.PublicKey,
            HsmSignature = hsmSignature,
            ValidFrom = hsmPayload.ValidFrom,
            ValidTo = hsmPayload.ValidTo,
        };
    }

    internal byte[] CreateEventSignature(EventSignaturePayload eventSignaturePayload, EcdsaPrivateKey key)
    {
        return _asymmetricAlgorithmAdapter.CreateSignature(eventSignaturePayload.ConvertToBytesToSign(), key);
    }

    internal EventSignaturePayload BuildEventSignaturePayload(
        Guid eventId,
        int signatureVersion,
        string streamName,
        Guid contestId,
        IMessage eventData,
        string machineName,
        string keyId,
        DateTime timestamp)
    {
        return new EventSignaturePayload(
            signatureVersion,
            eventId,
            streamName,
            _eventSerializer.Serialize(eventData).ToArray(),
            contestId,
            machineName,
            keyId,
            timestamp);
    }

    private EventSignatureMetadata CreateEventSignatureMetadata(
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

        var eventSignaturePayload = BuildEventSignaturePayload(
            eventId,
            EventSignatureVersions.V1,
            streamName,
            contestId,
            eventData,
            _machineConfig.Name,
            keyData.Key.Id,
            timestamp);

        var eventSignature = CreateEventSignature(eventSignaturePayload, keyData.Key);

        return new EventSignatureMetadata(
            contestId,
            EventSignatureVersions.V1,
            eventSignaturePayload.HostId,
            keyData.Key.Id,
            eventSignature);
    }
}
