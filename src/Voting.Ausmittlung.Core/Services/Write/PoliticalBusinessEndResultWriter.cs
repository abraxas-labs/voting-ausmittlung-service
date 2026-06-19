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
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.SecondFactor.Models;
using Voting.Lib.Iam.SecondFactor.Services;
using Voting.Lib.Iam.Services.ApiClient.Identity;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public abstract class PoliticalBusinessEndResultWriter<TAggregate, TEndResult>
    where TAggregate : BaseEventSourcingAggregate, IPoliticalBusinessEndResultAggregate
    where TEndResult : DataModels.PoliticalBusinessEndResultBase
{
    private readonly PermissionService _permissionService;
    private readonly ISecondFactorTransactionService _secondFactorTransactionService;

    protected PoliticalBusinessEndResultWriter(
        ILogger logger,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ContestService contestService,
        PermissionService permissionService,
        ISecondFactorTransactionService secondFactorTransactionService)
    {
        Logger = logger;
        AggregateRepository = aggregateRepository;
        AggregateFactory = aggregateFactory;
        ContestService = contestService;
        _permissionService = permissionService;
        _secondFactorTransactionService = secondFactorTransactionService;
    }

    protected ILogger Logger { get; }

    protected IAggregateRepository AggregateRepository { get; }

    protected IAggregateFactory AggregateFactory { get; }

    protected ContestService ContestService { get; }

    public async Task Finalize(Guid politicalBusinessId, Guid secondFactorTransactionId, string? otpCode = null, CancellationToken ct = default)
    {
        var (contestId, testingPhaseEnded) = await ContestService.EnsureNotLockedByPoliticalBusiness(politicalBusinessId);

        await _secondFactorTransactionService.EnsureVerified(
            secondFactorTransactionId,
            string.IsNullOrEmpty(otpCode) ? V1SecondFactorProvider.NEVIS : V1SecondFactorProvider.OTP,
            () => PrepareFinalizeActionId(politicalBusinessId, testingPhaseEnded),
            otpCode,
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

    public async Task<SecondFactorTransactionInfo> PrepareFinalize(Guid politicalBusinessId, string message)
    {
        await ContestService.EnsureNotLockedByPoliticalBusiness(politicalBusinessId);

        // Check if the user can access the end result
        _ = await GetEndResult(politicalBusinessId, _permissionService.TenantId)
            ?? throw new EntityNotFoundException(politicalBusinessId);

        var (_, testingPhaseEnded) = await ContestService.EnsureNotLockedByPoliticalBusiness(politicalBusinessId);
        var actionId = await PrepareFinalizeActionId(politicalBusinessId, testingPhaseEnded);
        return await _secondFactorTransactionService.Create(actionId, message);
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

    private async Task<IActionId> PrepareFinalizeActionId(Guid politicalBusinessId, bool testingPhaseEnded)
    {
        var endResultId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(politicalBusinessId, testingPhaseEnded);
        var aggregate = await AggregateRepository.GetOrCreateById<TAggregate>(endResultId);
        return aggregate.PrepareFinalize(politicalBusinessId, testingPhaseEnded);
    }
}
