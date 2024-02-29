// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Ausmittlung.TemporaryData.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Iam.Services;
using Voting.Lib.Iam.Services.ApiClient.Identity;

namespace Voting.Ausmittlung.Core.Services.Write;

public class SecondFactorTransactionWriter
{
    private readonly SecondFactorTransactionRepo _repo;
    private readonly IUserService _userService;
    private readonly PermissionService _permissionService;
    private readonly IClock _clock;
    private readonly AppConfig _appConfig;
    private readonly ILogger<SecondFactorTransactionWriter> _logger;
    private readonly IActionIdComparer _actionIdComparer;

    public SecondFactorTransactionWriter(
        SecondFactorTransactionRepo repo,
        IUserService userService,
        PermissionService permissionService,
        IClock clock,
        AppConfig appConfig,
        ILogger<SecondFactorTransactionWriter> logger,
        IActionIdComparer actionIdComparer)
    {
        _repo = repo;
        _userService = userService;
        _permissionService = permissionService;
        _clock = clock;
        _appConfig = appConfig;
        _logger = logger;
        _actionIdComparer = actionIdComparer;
    }

    public async Task<(SecondFactorTransaction SecondFactorTransaction, string Code)> CreateSecondFactorTransaction(ActionId actionId, string message)
    {
        var actionIdHash = actionId.ComputeHash();
        var userId = _permissionService.UserId;
        var code = RandomUtil.GetRandomString(4, "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray());
        message = $"({code}) {message}";
        var secondFactorAuthId = await _userService.RequestSecondFactor(
            userId,
            nameof(V1SecondFactorProvider.NEVIS),
            message,
            _appConfig.SecureConnect.Temporary2FATenantId);
        var now = _clock.UtcNow;
        var expiredAt = now.AddMinutes(_appConfig.Publisher.SecondFactorTransactionExpiredAtMinutes);
        var secondFactorTransaction = new SecondFactorTransaction
        {
            Id = Guid.NewGuid(),
            ActionId = actionIdHash,
            ExternalIdentifier = secondFactorAuthId,
            CreatedAt = now,
            LastUpdatedAt = now,
            ExpiredAt = expiredAt,
            UserId = userId,
        };
        await _repo.Create(secondFactorTransaction);
        _logger.LogInformation(
            SecurityLogging.SecurityEventId,
            "Created second factor transaction {SecondFactorExternalId} for action {ActionId}",
            secondFactorTransaction.ExternalIdentifier,
            actionId);
        return (secondFactorTransaction, code);
    }

    public async Task EnsureVerified(string externalId, Func<Task<ActionId>> action, CancellationToken ct)
    {
        await EnsureAwaitVerification(externalId, ct);

        // The action id must be fetched after the blocking verify request, to check for data changes in the aggregate during the request.
        var actionId = await action();
        await EnsureDataHasNotChanged(externalId, actionId);
        _logger.LogInformation(
            SecurityLogging.SecurityEventId,
            "Second factor transaction {SecondFactorExternalId} for action {ActionId} verified",
            externalId,
            actionId);
    }

    private async Task EnsureAwaitVerification(string externalId, CancellationToken ct)
    {
        var secondFactorTransaction = await _repo.GetByExternalIdentifier(externalId) ?? throw new EntityNotFoundException(externalId);
        secondFactorTransaction.PollCount++;
        secondFactorTransaction.LastUpdatedAt = _clock.UtcNow;
        await _repo.Update(secondFactorTransaction);

        var isVerified = await _userService.VerifySecondFactor(
            _permissionService.UserId,
            V1SecondFactorProvider.NEVIS,
            secondFactorTransaction.ExternalIdentifier,
            _appConfig.SecureConnect.Temporary2FATenantId,
            ct);
        if (!isVerified)
        {
            _logger.LogWarning(
                SecurityLogging.SecurityEventId,
                "Second factor transaction {SecondFactorExternalId} failed",
                externalId);
            throw new SecondFactorTransactionNotVerifiedException();
        }
    }

    private async Task EnsureDataHasNotChanged(string externalId, ActionId actionId)
    {
        var secondFactorTransaction = await _repo.GetByExternalIdentifier(externalId) ?? throw new EntityNotFoundException(externalId);
        if (!_actionIdComparer.Compare(actionId, secondFactorTransaction.ActionId))
        {
            _logger.LogWarning(
                SecurityLogging.SecurityEventId,
                "Data changed during second factor transaction {SecondFactorExternalId} for action {ActionId}",
                externalId,
                actionId);
            throw new SecondFactorTransactionDataChangedException();
        }
    }
}
