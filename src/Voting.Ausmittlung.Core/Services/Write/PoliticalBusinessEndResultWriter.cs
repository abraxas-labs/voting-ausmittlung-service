// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public abstract class PoliticalBusinessEndResultWriter<TAggregate, TEndResult>
    where TAggregate : BaseEventSourcingAggregate, IPoliticalBusinessEndResultAggregate
    where TEndResult : DataModels.PoliticalBusinessEndResultBase
{
    private readonly PermissionService _permissionService;
    private readonly SecondFactorTransactionWriter _secondFactorTransactionWriter;

    protected PoliticalBusinessEndResultWriter(
        ILogger logger,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ContestService contestService,
        PermissionService permissionService,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
    {
        Logger = logger;
        AggregateRepository = aggregateRepository;
        AggregateFactory = aggregateFactory;
        ContestService = contestService;
        _permissionService = permissionService;
        _secondFactorTransactionWriter = secondFactorTransactionWriter;
    }

    protected ILogger Logger { get; }

    protected IAggregateRepository AggregateRepository { get; }

    protected IAggregateFactory AggregateFactory { get; }

    protected ContestService ContestService { get; }

    public async Task Finalize(Guid politicalBusinessId, Guid secondFactorTransactionId, CancellationToken ct)
    {
        var (contestId, testingPhaseEnded) = await ContestService.EnsureNotLockedByPoliticalBusiness(politicalBusinessId);

        await _secondFactorTransactionWriter.EnsureVerified(
            secondFactorTransactionId,
            () => PrepareFinalizeActionId(politicalBusinessId, testingPhaseEnded),
            ct);

        var endResult = await GetEndResult(politicalBusinessId, _permissionService.TenantId)
            ?? throw new EntityNotFoundException(politicalBusinessId);

        await ValidateFinalize(endResult, contestId);

        var aggregate = await AggregateRepository.GetOrCreateById<TAggregate>(endResult.Id);
        aggregate.Finalize(politicalBusinessId, contestId, testingPhaseEnded);
        await AggregateRepository.Save(aggregate);
        Logger.LogInformation("Finalized end result {EndResultId} of type {EndResultType}", endResult.Id, typeof(TEndResult).Name);
    }

    public async Task RevertFinalization(Guid politicalBusinessId)
    {
        var (contestId, _) = await ContestService.EnsureNotLockedByPoliticalBusiness(politicalBusinessId);

        var endResult = await GetEndResult(politicalBusinessId, _permissionService.TenantId)
            ?? throw new EntityNotFoundException(politicalBusinessId);

        await ValidateRevertFinalize(endResult, contestId);

        var aggregate = await AggregateRepository.GetById<TAggregate>(endResult.Id);
        aggregate.RevertFinalization(contestId);
        await AggregateRepository.Save(aggregate);
        Logger.LogInformation(
            "Reverted finalization of end result {EndResultId} of type {EndResultType}",
            endResult.Id,
            typeof(TEndResult).Name);
    }

    public async Task<(SecondFactorTransaction SecondFactorTransaction, string Code, string QrCode)> PrepareFinalize(Guid politicalBusinessId, string message)
    {
        await ContestService.EnsureNotLockedByPoliticalBusiness(politicalBusinessId);

        // Check if the user can access the end result
        _ = await GetEndResult(politicalBusinessId, _permissionService.TenantId)
            ?? throw new EntityNotFoundException(politicalBusinessId);

        var (_, testingPhaseEnded) = await ContestService.EnsureNotLockedByPoliticalBusiness(politicalBusinessId);
        var actionId = await PrepareFinalizeActionId(politicalBusinessId, testingPhaseEnded);
        return await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
    }

    protected abstract Task<TEndResult?> GetEndResult(Guid politicalBusinessId, string tenantId);

    protected virtual async Task ValidateFinalize(TEndResult endResult, Guid contestId)
    {
        await ContestService.EnsureEndResultFinalizeEnabled(contestId);

        if (!endResult.AllCountingCirclesDone)
        {
            throw new ValidationException("not all counting circles are done");
        }
    }

    protected virtual async Task ValidateRevertFinalize(TEndResult endResult, Guid contestId)
    {
        await ContestService.EnsureEndResultFinalizeEnabled(contestId);
    }

    private async Task<ActionId> PrepareFinalizeActionId(Guid politicalBusinessId, bool testingPhaseEnded)
    {
        var endResultId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(politicalBusinessId, testingPhaseEnded);
        var aggregate = await AggregateRepository.GetOrCreateById<TAggregate>(endResultId);
        return aggregate.PrepareFinalize(politicalBusinessId, testingPhaseEnded);
    }
}
