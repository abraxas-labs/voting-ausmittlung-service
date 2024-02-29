// (c) Copyright 2024 by Abraxas Informatik AG
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

    internal async IAsyncEnumerable<CountingCircleResultAggregate> EnsureAllCountingCirclesInSubmissionOrCorrection(
        Guid contestId,
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
            switch (aggregate.State)
            {
                case CountingCircleResultState.Initial:
                case CountingCircleResultState.SubmissionOngoing:
                case CountingCircleResultState.ReadyForCorrection:
                    continue;
                case CountingCircleResultState.SubmissionDone:
                case CountingCircleResultState.CorrectionDone:
                    aggregate.FlagForCorrection(contestId);
                    yield return aggregate;
                    break;
                default:
                    throw new CountingCircleResultInInvalidStateForEVotingImportException(aggregate.Id);
            }
        }
    }

    protected Task<TAggregate?> GetAggregate(Guid id)
        => _aggregateRepository.TryGetById<TAggregate>(id);

    protected abstract IQueryable<TResult> BuildResultsQuery(Guid contestId);
}
