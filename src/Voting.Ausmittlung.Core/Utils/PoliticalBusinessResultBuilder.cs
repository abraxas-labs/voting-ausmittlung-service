// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public abstract class PoliticalBusinessResultBuilder<TResult>
    where TResult : CountingCircleResult
{
    private readonly IDbRepository<DataContext, CountingCircleResultComment> _resultCommentRepo;

    protected PoliticalBusinessResultBuilder(
        SimpleCountingCircleResultRepo simpleResultRepo,
        IDbRepository<DataContext, CountingCircleResultComment> resultCommentRepo)
    {
        SimpleResultRepo = simpleResultRepo;
        _resultCommentRepo = resultCommentRepo;
    }

    protected SimpleCountingCircleResultRepo SimpleResultRepo { get; }

    protected async Task UpdateSimpleResult(Guid resultId, PoliticalBusinessNullableCountOfVoters countOfVoters)
    {
        var simpleResult = await SimpleResultRepo.GetByKey(resultId)
                           ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        simpleResult.CountOfVoters = countOfVoters;
        await SimpleResultRepo.Update(simpleResult);
    }

    protected async Task ResetSimpleResult(Guid resultId, VotingDataSource dataSource, bool includeCountOfVoters, TResult result)
    {
        var simpleResult = await SimpleResultRepo.GetByKey(resultId)
                           ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);
        await ResetSimpleResult(simpleResult, dataSource, includeCountOfVoters, result);
    }

    protected async Task ResetSimpleResults(
        IEnumerable<TResult> results,
        VotingDataSource dataSource)
    {
        var resultsById = results.ToDictionary(x => x.Id);
        var resultIds = resultsById.Keys.ToHashSet();
        var simpleResults = await SimpleResultRepo.Query()
            .Where(x => resultIds.Contains(x.Id))
            .ToListAsync();
        foreach (var simpleResult in simpleResults)
        {
            await ResetSimpleResult(simpleResult, dataSource, true, resultsById[simpleResult.Id]);
        }
    }

    protected virtual async Task ResetSimpleResult(
        SimpleCountingCircleResult simpleResult,
        VotingDataSource dataSource,
        bool includeCountOfVoters,
        TResult result)
    {
        if (includeCountOfVoters)
        {
            simpleResult.CountOfVoters.ResetSubTotal(dataSource, result.TotalCountOfVoters);
        }

        simpleResult.HasComments = false;
        await SimpleResultRepo.Update(simpleResult);
        await _resultCommentRepo.Query()
            .Where(x => x.ResultId == simpleResult.Id)
            .ExecuteDeleteAsync();
    }
}
