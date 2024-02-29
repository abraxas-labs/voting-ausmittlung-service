// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.EventSignature;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Subscribe;

namespace Voting.Ausmittlung.Core.EventProcessors.Transient;

/// <summary>
/// Transient catch up processor for contest events.
/// When a event is processed in catch up mode, only contest properties need to be updated.
/// Signature relevant data will not be changed during catch up, because it is unknown whether the signature is really needed or not.
/// After the catch up is completed, it will generate signature keys according the current contest cache state.
/// When a events is live processed, it will also update signature relevant data.
/// </summary>
public class TransientCatchUpContestProcessor :
    ITransientCatchUpDetectorEventProcessor<ContestCreated>,
    ITransientCatchUpDetectorEventProcessor<ContestUpdated>,
    ITransientCatchUpDetectorEventProcessor<ContestDeleted>,
    ITransientCatchUpDetectorEventProcessor<ContestTestingPhaseEnded>,
    ITransientCatchUpDetectorEventProcessor<ContestPastLocked>,
    ITransientCatchUpDetectorEventProcessor<ContestPastUnlocked>,
    ITransientCatchUpDetectorEventProcessor<ContestArchived>,
    ITransientEventProcessorCatchUpCompleter
{
    private readonly ContestCache _contestCache;
    private readonly EventSignatureService _eventSignatureService;
    private readonly ILogger<TransientCatchUpContestProcessor> _logger;

    public TransientCatchUpContestProcessor(
        ContestCache contestCache,
        EventSignatureService eventSignatureService,
        ILogger<TransientCatchUpContestProcessor> logger)
    {
        _contestCache = contestCache;
        _eventSignatureService = eventSignatureService;
        _logger = logger;
    }

    public Task Process(ContestCreated eventData, bool isCatchUp)
    {
        using var cacheWrite = _contestCache.BatchWrite();

        var date = eventData.Contest.Date.ToDateTime();
        var contestId = GuidParser.Parse(eventData.Contest.Id);

        if (_contestCache.ContainsKey(contestId))
        {
            LogPossibleReplayAttackDetected("Contest {ContestId} already in the Cache", contestId);
            return Task.CompletedTask;
        }

        var entry = new ContestCacheEntry
        {
            Id = contestId,
            Date = date,
            PastLockedPer = date.NextUtcDate(true),
            State = ContestState.TestingPhase,
        };

        _contestCache.Add(entry);
        return Task.CompletedTask;
    }

    public Task Process(ContestUpdated eventData, bool isCatchUp)
    {
        // A update is only possible in testing phase.
        using var cacheWrite = _contestCache.BatchWrite();

        var date = eventData.Contest.Date.ToDateTime();
        var contestId = GuidParser.Parse(eventData.Contest.Id);

        if (!_contestCache.TryGet(contestId, out var entry))
        {
            LogContestNotFoundWarning(contestId);
            return Task.CompletedTask;
        }

        entry.Date = date;
        entry.PastLockedPer = date.NextUtcDate(true);

        return Task.CompletedTask;
    }

    public async Task Process(ContestTestingPhaseEnded eventData, bool isCatchUp)
    {
        using var cacheWrite = _contestCache.BatchWrite();

        var contestId = GuidParser.Parse(eventData.ContestId);
        if (!_contestCache.TryGet(contestId, out var entry))
        {
            LogContestNotFoundWarning(contestId);
            return;
        }

        LogContestStateChange(entry, ContestState.Active);

        if (!ValidateContestHasNoActiveSignature(entry))
        {
            return;
        }

        var keyData = isCatchUp ? null : await _eventSignatureService.StartSignature(entry.Id, entry.PastLockedPer);
        entry.State = ContestState.Active;
        entry.KeyData = keyData;
    }

    public async Task Process(ContestPastUnlocked eventData, bool isCatchUp)
    {
        using var cacheWrite = _contestCache.BatchWrite();

        var contestId = GuidParser.Parse(eventData.ContestId);
        if (!_contestCache.TryGet(contestId, out var entry))
        {
            LogContestNotFoundWarning(contestId);
            return;
        }

        LogContestStateChange(entry, ContestState.PastUnlocked);

        if (!ValidateContestHasNoActiveSignature(entry))
        {
            return;
        }

        var pastLockedPer = eventData.EventInfo.Timestamp.ToDateTime().NextUtcDate(true); // copied from voting basis event processor.
        var keyData = isCatchUp ? null : await _eventSignatureService.StartSignature(entry.Id, pastLockedPer);
        entry.PastLockedPer = pastLockedPer;
        entry.State = ContestState.PastUnlocked;
        entry.KeyData = keyData;
    }

    public async Task Process(ContestPastLocked eventData, bool isCatchUp)
    {
        using var cacheWrite = _contestCache.BatchWrite();

        var contestId = GuidParser.Parse(eventData.ContestId);
        if (!_contestCache.TryGet(contestId, out var entry))
        {
            LogContestNotFoundWarning(contestId);
            return;
        }

        LogContestStateChange(entry, ContestState.PastLocked);

        if (!isCatchUp)
        {
            if (!ValidateContestHasKeyData(entry))
            {
                return;
            }

            await _eventSignatureService.StopSignature(entry.Id, entry.KeyData!.Key.Id);
            entry.KeyData!.Key.Dispose();
            entry.KeyData = null;
        }

        entry.State = ContestState.PastLocked;
    }

    public Task Process(ContestArchived eventData, bool isCatchUp)
    {
        using var cacheWrite = _contestCache.BatchWrite();

        var contestId = GuidParser.Parse(eventData.ContestId);
        if (!_contestCache.TryGet(contestId, out var entry))
        {
            LogContestNotFoundWarning(contestId);
            return Task.CompletedTask;
        }

        LogContestStateChange(entry, ContestState.Archived);

        // A contest which is to be archived should never have an active signature.
        // The state transition to archived is only possible from past locked.
        if (!ValidateContestHasNoActiveSignature(entry))
        {
            return Task.CompletedTask;
        }

        _contestCache.Remove(contestId);
        return Task.CompletedTask;
    }

    public Task Process(ContestDeleted eventData, bool isCatchUp)
    {
        using var cacheWrite = _contestCache.BatchWrite();

        var contestId = GuidParser.Parse(eventData.ContestId);
        if (!_contestCache.TryGet(contestId, out var entry))
        {
            LogContestNotFoundWarning(contestId);
            return Task.CompletedTask;
        }

        // A contest which is to be deleted should never have an active signature.
        // A delete is only possible if the contest is in testing phase.
        if (!ValidateContestHasNoActiveSignature(entry))
        {
            return Task.CompletedTask;
        }

        _contestCache.Remove(contestId);
        return Task.CompletedTask;
    }

    public async Task CatchUpCompleted()
    {
        using var cacheWrite = _contestCache.BatchWrite();

        var keyDataByContestId = new Dictionary<Guid, ContestCacheEntryKeyData>();

        try
        {
            var entries = _contestCache.GetAll().Where(c => c.State.IsActiveOrUnlocked()).ToList();

            foreach (var entry in entries)
            {
                ValidateContestNoKeyData(entry);
                var keyData = await _eventSignatureService.StartSignature(entry.Id, entry.PastLockedPer);
                keyDataByContestId.Add(entry.Id, keyData);
            }

            foreach (var entry in entries)
            {
                entry.KeyData = keyDataByContestId[entry.Id];
            }
        }
        catch (Exception ex)
        {
            foreach (var keyData in keyDataByContestId.Values)
            {
                // no need to set any reference to null, because keyData
                // is not assigned to the contest cache entry yet and keyDataByContestId is a local variable.
                keyData.Key.Dispose();
            }

            _logger.LogError(ex, "Could not complete the catch up");
            throw;
        }
    }

    private bool ValidateContestHasNoActiveSignature(ContestCacheEntry entry)
    {
        if (entry.State.IsActiveOrUnlocked())
        {
            LogPossibleReplayAttackDetected("Cannot process because contest cache entry {ContestId} has the state {ContestState} but it should not be active or unlocked", entry.Id, entry.State);
            return false;
        }

        return ValidateContestNoKeyData(entry);
    }

    private bool ValidateContestNoKeyData(ContestCacheEntry entry)
    {
        if (entry.KeyData != null)
        {
            LogPossibleReplayAttackDetected("Cannot process because contest cache entry {ContestId} has key which is not disposed correctly", entry.Id);
            return false;
        }

        return true;
    }

    private bool ValidateContestHasKeyData(ContestCacheEntry entry)
    {
        if (entry.KeyData == null)
        {
            LogPossibleReplayAttackDetected("No key assigned for contest cache entry {ContestId}. Cannot stop signature", entry.Id);
            return false;
        }

        return true;
    }

    private void LogContestStateChange(ContestCacheEntry entry, ContestState nextState)
    {
        _logger.LogInformation("Transient catch up contest cache entry {ContestId} state changed from {PreviousState} to {NextState}", entry.Id, entry.State, nextState);
    }

    private void LogContestNotFoundWarning(Guid contestId)
    {
        LogPossibleReplayAttackDetected("Contest {ContestId} not found in the Cache", contestId);
    }

    private void LogPossibleReplayAttackDetected(string message, params object?[] args)
    {
        _logger.LogWarning(SecurityLogging.SecurityEventId, message + ". Possible replay attack detected.", args);
    }
}
