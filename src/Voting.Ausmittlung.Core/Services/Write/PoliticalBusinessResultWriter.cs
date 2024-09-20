// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Core.Services.Write;

public abstract class PoliticalBusinessResultWriter<T>
    where T : CountingCircleResult
{
    private readonly PermissionService _permissionService;
    private readonly ContestService _contestService;

    protected PoliticalBusinessResultWriter(
        PermissionService permissionService,
        ContestService contestService,
        IAuth auth,
        IAggregateRepository aggregateRepository)
    {
        _permissionService = permissionService;
        _contestService = contestService;
        Auth = auth;
        AggregateRepository = aggregateRepository;
    }

    protected IAggregateRepository AggregateRepository { get; }

    protected IAuth Auth { get; }

    /// <summary>
    /// Ensures that the current user has write permissions on the political business result.
    /// </summary>
    /// <param name="resultId">The political business result ID.</param>
    /// <returns>The contest ID of the political business result.</returns>
    /// <exception cref="ValidationException">Thrown if the political business is not yet active.</exception>
    /// <exception cref="Core.Exceptions.ContestLockedException">Thrown if the contest is in a locked state.</exception>
    /// <exception cref="ForbiddenException">Thrown if the current user does not have write permissions on the political business result.</exception>
    protected async Task<Guid> EnsurePoliticalBusinessPermissions(Guid resultId)
    {
        return await EnsurePoliticalBusinessPermissions(await LoadPoliticalBusinessResult(resultId));
    }

    /// <summary>
    /// Ensures that the current user has write permissions on the political business result.
    /// Note that this method is only applicable to VOTING Ausmittlung Erfassung.
    /// For VOTING Ausmittlung Monitoring, <see cref="EnsurePoliticalBusinessPermissionsForMonitor"/> should be used.
    /// </summary>
    /// <param name="result">The political business result.</param>
    /// <returns>The contest ID of the political business result.</returns>
    /// <exception cref="ValidationException">Thrown if the political business is not yet active.</exception>
    /// <exception cref="Core.Exceptions.ContestLockedException">Thrown if the contest is in a locked state.</exception>
    /// <exception cref="ForbiddenException">Thrown if the current user does not have write permissions on the political business result.</exception>
    protected async Task<Guid> EnsurePoliticalBusinessPermissions(T result)
    {
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircle(result.CountingCircleId, result.PoliticalBusiness.ContestId);

        if (!result.PoliticalBusiness.Active)
        {
            throw new ValidationException("political business is not active");
        }

        _contestService.EnsureNotLocked(result.PoliticalBusiness.Contest);
        return result.PoliticalBusiness.ContestId;
    }

    /// <summary>
    /// Ensures that the current user has write permissions on the political business result.
    /// Note that this method is only applicable to VOTING Ausmittlung Monitoring.
    /// For VOTING Ausmittlung Erfassung, <see cref="EnsurePoliticalBusinessPermissions(System.Guid)"/> should be used.
    /// </summary>
    /// <param name="resultId">The political business result ID.</param>
    /// <returns>The contest ID of the political business result.</returns>
    /// <exception cref="ValidationException">Thrown if the political business is not yet active.</exception>
    /// <exception cref="Core.Exceptions.ContestLockedException">Thrown if the contest is in a locked state.</exception>
    /// <exception cref="ForbiddenException">Thrown if the current user does not have write permissions on the political business result.</exception>
    protected async Task<Guid> EnsurePoliticalBusinessPermissionsForMonitor(Guid resultId)
    {
        var result = await LoadPoliticalBusinessResult(resultId);

        if (result.PoliticalBusiness.DomainOfInfluence.SecureConnectId != _permissionService.TenantId)
        {
            throw new ForbiddenException("cannot edit political business result");
        }

        if (!result.PoliticalBusiness.Active)
        {
            throw new ValidationException("political business is not active");
        }

        _contestService.EnsureNotLocked(result.PoliticalBusiness.Contest);
        return result.PoliticalBusiness.ContestId;
    }

    /// <summary>
    /// Execute an action on multiple aggregates.
    /// </summary>
    /// <param name="aggregateIds">The aggregate IDs on which to perform the action.</param>
    /// <param name="action">The action to perform.</param>
    /// <typeparam name="TAggregate">The type of the aggregates.</typeparam>
    /// <exception cref="ValidationException">Thrown if duplicate IDs are present.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task ExecuteOnAllAggregates<TAggregate>(IReadOnlyCollection<Guid> aggregateIds, Func<TAggregate, Task> action)
        where TAggregate : BaseEventSourcingAggregate
    {
        if (aggregateIds.Distinct().Count() != aggregateIds.Count)
        {
            throw new ValidationException("duplicate ids present");
        }

        // Perform the action first to make sure no validation exceptions or similar occur.
        var aggregates = new List<TAggregate>();
        foreach (var id in aggregateIds)
        {
            var aggregate = await AggregateRepository.GetById<TAggregate>(id);
            await action(aggregate);
            aggregates.Add(aggregate);
        }

        foreach (var aggregate in aggregates)
        {
            await AggregateRepository.Save(aggregate);
        }
    }

    /// <summary>
    /// Gets an action id of the aggregate provided by the type argument and id concat by the ContestCountingCircleDetailsAggregate.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="resultId">The id of the result.</param>
    /// <param name="contestId">The id of the contest.</param>
    /// <param name="countingCircleId">The id of the counting circle.</param>
    /// <param name="testingPhaseEnded">Whether the contest is in testing phase or not.</param>
    /// <typeparam name="TResultAggregate">The type of the result aggregate.</typeparam>
    /// <returns>The action id.</returns>
    protected async Task<ActionId> PrepareActionId<TResultAggregate>(string action, Guid resultId, Guid contestId, Guid countingCircleId, bool testingPhaseEnded)
        where TResultAggregate : BaseEventSignatureAggregate
    {
        var resultAggregate = await AggregateRepository.GetById<TResultAggregate>(resultId);

        var id = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, countingCircleId, testingPhaseEnded);
        var detailsAggregate = await AggregateRepository.TryGetById<ContestCountingCircleDetailsAggregate>(id)
                               ?? throw new ValidationException("Counting circle details aggregate is not initialized yet");

        return new ActionId(action, resultAggregate, detailsAggregate);
    }

    protected bool IsSelfOwnedPoliticalBusiness(PoliticalBusiness politicalBusiness)
    {
        return politicalBusiness.DomainOfInfluence.SecureConnectId == Auth.Tenant.Id;
    }

    protected async Task EnsureStatePlausibilisedEnabled(Guid contestId) =>
        await _contestService.EnsureStatePlausibilisedEnabled(contestId);

    protected bool CanUnpublishResults(Contest contest, CountingCircleResultAggregate aggregate)
    {
        if (!aggregate.Published)
        {
            return false;
        }

        return !contest.CantonDefaults.PublishResultsBeforeAuditedTentatively
            ? aggregate.State == CountingCircleResultState.AuditedTentatively
            : aggregate.State is CountingCircleResultState.SubmissionDone or CountingCircleResultState.CorrectionDone;
    }

    protected bool CanAutomaticallyPublishResults(DomainOfInfluence domainOfInfluence, Contest contest, CountingCircleResultAggregate aggregate)
    {
        if (aggregate.Published)
        {
            return false;
        }

        var validCcResultState = !contest.CantonDefaults.PublishResultsBeforeAuditedTentatively
            ? aggregate.State == CountingCircleResultState.AuditedTentatively
            : aggregate.State is CountingCircleResultState.SubmissionDone or CountingCircleResultState.CorrectionDone or CountingCircleResultState.AuditedTentatively;

        // publish results if not enabled or for all results which have domain of influence type MU or lower
        return validCcResultState && (!contest.CantonDefaults.PublishResultsEnabled || domainOfInfluence.Type >= DomainOfInfluenceType.Mu);
    }

    protected void EnsureCanManuallyPublishResults(Contest contest, DomainOfInfluence domainOfInfluence, CountingCircleResultAggregate aggregate)
    {
        if (!contest.CantonDefaults.PublishResultsEnabled)
        {
            throw new ValidationException("publish results is not enabled for contest");
        }

        if (!contest.CantonDefaults.PublishResultsBeforeAuditedTentatively
            && aggregate.State != CountingCircleResultState.AuditedTentatively
            && aggregate.State != CountingCircleResultState.Plausibilised)
        {
            throw new ValidationException("cannot publish or unpublish a result with the state " + aggregate.State);
        }

        if (contest.CantonDefaults.PublishResultsBeforeAuditedTentatively
            && aggregate.State != CountingCircleResultState.SubmissionDone
            && aggregate.State != CountingCircleResultState.CorrectionDone
            && aggregate.State != CountingCircleResultState.AuditedTentatively
            && aggregate.State != CountingCircleResultState.Plausibilised)
        {
            throw new ValidationException("cannot publish or unpublish a result with the state " + aggregate.State);
        }

        if (domainOfInfluence.Type >= DomainOfInfluenceType.Mu)
        {
            throw new ValidationException($"cannot publish or unpublish results for domain of influence type {DomainOfInfluenceType.Mu} or lower");
        }
    }

    protected abstract Task<T> LoadPoliticalBusinessResult(Guid resultId);
}
