// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public abstract class PoliticalBusinessResultImportWriter<TAggregate, TResult>
    where TAggregate : CountingCircleResultAggregate
    where TResult : CountingCircleResult
{
    private readonly IAggregateRepository _aggregateRepository;

    protected PoliticalBusinessResultImportWriter(IAggregateRepository aggregateRepository)
    {
        _aggregateRepository = aggregateRepository;
    }

    internal async Task<int> EnsureInSubmissionOrCorrection(List<Guid> resultIds)
    {
        var aggregateLoadingTasks = resultIds.Select(GetAggregate);
        var aggregates = (await Task.WhenAll(aggregateLoadingTasks)).WhereNotNull().ToList();
        var firstInvalidAggregate = aggregates.Find(x => x.State
                is not CountingCircleResultState.Initial
                and not CountingCircleResultState.SubmissionOngoing
                and not CountingCircleResultState.ReadyForCorrection);
        if (firstInvalidAggregate != null)
        {
            throw new CountingCircleResultInInvalidStateForImportException(firstInvalidAggregate.Id);
        }

        return aggregates.Count;
    }

    internal async IAsyncEnumerable<CountingCircleResultAggregate> SetAllToInSubmissionOrCorrection(
        Guid contestId,
        bool testingPhaseEnded,
        IReadOnlyCollection<Guid> countingCircleIds)
    {
        var resultIds = await BuildResultsQuery(contestId)
            .Where(x => countingCircleIds.Contains(x.CountingCircleId)
                        && x.State != CountingCircleResultState.Initial
                        && x.State != CountingCircleResultState.SubmissionOngoing
                        && x.State != CountingCircleResultState.ReadyForCorrection)
            .Select(x => x.Id)
            .ToListAsync();

        var aggregateLoadingTasks = resultIds.Select(GetAggregate);

        var aggregates = await Task.WhenAll(aggregateLoadingTasks);
        foreach (var aggregate in aggregates.WhereNotNull())
        {
            FlagForCorrection(aggregate, testingPhaseEnded, contestId);
            if (aggregate.GetUncommittedEvents().Count != 0)
            {
                yield return aggregate;
            }
        }
    }

    protected Task<TAggregate?> GetAggregate(Guid id)
        => _aggregateRepository.TryGetById<TAggregate>(id);

    protected abstract IQueryable<TResult> BuildResultsQuery(Guid contestId);

    private void FlagForCorrection(CountingCircleResultAggregate aggregate, bool testingPhaseEnded, Guid contestId)
    {
        if (testingPhaseEnded && aggregate.State is CountingCircleResultState.AuditedTentatively or CountingCircleResultState.Plausibilised)
        {
            throw new CountingCircleResultInInvalidStateForImportException(aggregate.Id);
        }

        switch (aggregate.State)
        {
            case CountingCircleResultState.SubmissionDone:
            case CountingCircleResultState.CorrectionDone:
                aggregate.FlagForCorrection(contestId);
                break;
            case CountingCircleResultState.AuditedTentatively:
                aggregate.ResetToSubmissionFinished(contestId);
                aggregate.FlagForCorrection(contestId);
                break;
            case CountingCircleResultState.Plausibilised:
                aggregate.ResetToAuditedTentatively(contestId);
                aggregate.ResetToSubmissionFinished(contestId);
                aggregate.FlagForCorrection(contestId);
                break;
        }
    }
}
