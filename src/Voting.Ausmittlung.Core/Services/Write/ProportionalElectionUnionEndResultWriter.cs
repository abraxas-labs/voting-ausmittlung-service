// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ProportionalElectionUnionEndResultWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactoriy;
    private readonly ContestService _contestService;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionUnionEndResult> _endResultRepo;
    private readonly PermissionService _permissionService;
    private readonly ILogger<ProportionalElectionUnionEndResultWriter> _logger;
    private readonly SecondFactorTransactionWriter _secondFactorTransactionWriter;

    public ProportionalElectionUnionEndResultWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactoriy,
        ContestService contestService,
        IDbRepository<DataContext, DataModels.ProportionalElectionUnionEndResult> endResultRepo,
        PermissionService permissionService,
        ILogger<ProportionalElectionUnionEndResultWriter> logger,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactoriy = aggregateFactoriy;
        _contestService = contestService;
        _endResultRepo = endResultRepo;
        _permissionService = permissionService;
        _logger = logger;
        _secondFactorTransactionWriter = secondFactorTransactionWriter;
    }

    public async Task<(SecondFactorTransaction SecondFactorTransaction, string Code)> PrepareFinalize(Guid unionId, string message)
    {
        await _contestService.EnsureNotLockedByProportionalElectionUnion(unionId);

        // Check if the user can access the end result
        if (!await _endResultRepo.Query().AnyAsync(e => e.ProportionalElectionUnionId == unionId && e.ProportionalElectionUnion.SecureConnectId == _permissionService.TenantId))
        {
            throw new EntityNotFoundException(unionId);
        }

        var (_, testingPhaseEnded) = await _contestService.EnsureNotLockedByProportionalElectionUnion(unionId);
        var actionId = await PrepareFinalizeActionId(unionId, testingPhaseEnded);
        return await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
    }

    public async Task Finalize(Guid unionId, string secondFactorTransactionExternalId, CancellationToken ct)
    {
        var (contestId, testingPhaseEnded) = await _contestService.EnsureNotLockedByProportionalElectionUnion(unionId);

        await _secondFactorTransactionWriter.EnsureVerified(secondFactorTransactionExternalId, () => PrepareFinalizeActionId(unionId, testingPhaseEnded), ct);

        var endResult = await GetEndResult(unionId, _permissionService.TenantId)
                        ?? throw new EntityNotFoundException(unionId);

        await ValidateFinalize(endResult);

        var aggregate = await _aggregateRepository.GetOrCreateById<ProportionalElectionUnionEndResultAggregate>(endResult.Id);
        aggregate.Finalize(unionId, contestId, testingPhaseEnded);
        await _aggregateRepository.Save(aggregate);
        _logger.LogInformation("Finalized end result {EndResultId} of type {EndResultType}", endResult.Id, nameof(DataModels.ProportionalElectionUnionEndResult));
    }

    public async Task RevertFinalization(Guid unionId)
    {
        var (contestId, _) = await _contestService.EnsureNotLockedByProportionalElectionUnion(unionId);

        var endResult = await GetEndResult(unionId, _permissionService.TenantId)
                        ?? throw new EntityNotFoundException(unionId);

        var aggregate = await _aggregateRepository.GetById<ProportionalElectionUnionEndResultAggregate>(endResult.Id);
        aggregate.RevertFinalization(contestId);
        await _aggregateRepository.Save(aggregate);
        _logger.LogInformation(
            "Reverted finalization of end result {EndResultId} of type {EndResultType}",
            endResult.Id,
            nameof(DataModels.ProportionalElectionUnionEndResult));
    }

    protected virtual Task ValidateFinalize(DataModels.ProportionalElectionUnionEndResult endResult)
    {
        var mandateAlgorithm = endResult.ProportionalElectionUnion.ProportionalElectionUnionEntries.Select(x => x.ProportionalElection)
            .FirstOrDefault()?.MandateAlgorithm;

        if (!mandateAlgorithm.GetValueOrDefault().IsUnionDoubleProportional())
        {
            throw new ValidationException("Can only finalize unions with a union double proportional mandate algorithm");
        }

        if (!endResult.AllElectionsDone)
        {
            throw new ValidationException("Not all elections are done");
        }

        return Task.CompletedTask;
    }

    private async Task<ActionId> PrepareFinalizeActionId(Guid unionId, bool testingPhaseEnded)
    {
        var endResultId = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(unionId, testingPhaseEnded);
        var aggregate = await _aggregateRepository.GetOrCreateById<ProportionalElectionUnionEndResultAggregate>(endResultId);
        return aggregate.PrepareFinalize(unionId, testingPhaseEnded);
    }

    private Task<DataModels.ProportionalElectionUnionEndResult?> GetEndResult(Guid unionId, string tenantId)
    {
        return _endResultRepo.Query()
            .Include(x => x.ProportionalElectionUnion.ProportionalElectionUnionEntries)
            .ThenInclude(x => x.ProportionalElection)
            .FirstOrDefaultAsync(x =>
                x.ProportionalElectionUnionId == unionId &&
                x.ProportionalElectionUnion.SecureConnectId == tenantId);
    }
}
