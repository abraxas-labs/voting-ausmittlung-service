// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Exceptions;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write;

public class EventSignatureWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;
    private readonly ILogger<EventSignatureWriter> _logger;
    private readonly AppConfig _config;

    public EventSignatureWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService,
        ILogger<EventSignatureWriter> logger,
        AppConfig config)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _logger = logger;
        _config = config;
    }

    internal Task CreatePublicKey(EventSignaturePublicKeySignature signature)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();
        return RetryOnVersionMismatchException(async () =>
        {
            var aggregate = await _aggregateRepository.TryGetById<ContestEventSignatureAggregate>(signature.ContestId)
                ?? _aggregateFactory.New<ContestEventSignatureAggregate>();

            aggregate.CreatePublicKey(signature);
            await _aggregateRepository.Save(aggregate);
        });
    }

    internal Task DeletePublicKey(Guid contestId, string keyId, string hostId)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();
        return RetryOnVersionMismatchException(async () =>
        {
            var aggregate = await _aggregateRepository.GetById<ContestEventSignatureAggregate>(contestId);
            aggregate.DeletePublicKey(keyId, hostId);
            await _aggregateRepository.Save(aggregate);
        });
    }

    private async Task RetryOnVersionMismatchException(Func<Task> action)
    {
        for (var i = 1; i <= _config.EventSignature.EventWritesMaxAttempts; i++)
        {
            try
            {
                await action();
                return;
            }
            catch (VersionMismatchException e) when (i < _config.EventSignature.EventWritesMaxAttempts)
            {
                _logger.LogInformation(e, "Version mismatch when trying to write event signature event, attempt {AttemptNr} of {TotalAttempts}", i, _config.EventSignature.EventWritesMaxAttempts);
                await Task.Delay(Random.Shared.Next(_config.EventSignature.EventWritesRetryMinDelayMillis, _config.EventSignature.EventWritesRetryMaxDelayMillis));
            }
        }
    }
}
