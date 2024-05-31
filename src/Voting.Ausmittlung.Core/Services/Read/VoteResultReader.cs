// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class VoteResultReader
{
    private readonly IDbRepository<DataContext, VoteResult> _repo;
    private readonly PermissionService _permissionService;

    public VoteResultReader(
        IDbRepository<DataContext, VoteResult> repo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
    }

    public async Task<VoteResult> Get(Guid voteResultId)
    {
        return await QueryVoteResult(x => x.Id == voteResultId)
               ?? throw new EntityNotFoundException(voteResultId);
    }

    public async Task<VoteResult> Get(Guid voteId, Guid basisCountingCircleId)
    {
        return await QueryVoteResult(x => x.VoteId == voteId && x.CountingCircle.BasisCountingCircleId == basisCountingCircleId)
               ?? throw new EntityNotFoundException(new { voteId, basisCountingCircleId });
    }

    private async Task<VoteResult?> QueryVoteResult(Expression<Func<VoteResult, bool>> predicate)
    {
        var voteResult = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.Translations)
            .Include(x => x.Vote.Contest.Translations)
            .Include(x => x.Vote.Contest.CantonDefaults)
            .Include(x => x.Vote.DomainOfInfluence)
            .Include(x => x.CountingCircle)
            .Include(v => v.Results).ThenInclude(r => r.Ballot)
            .Include(v => v.Results).ThenInclude(r => r.QuestionResults).ThenInclude(qr => qr.Question.Translations)
            .Include(v => v.Results).ThenInclude(r => r.TieBreakQuestionResults).ThenInclude(qr => qr.Question.Translations)
            .Where(predicate)
            .FirstOrDefaultAsync();

        if (voteResult == null)
        {
            return null;
        }

        await _permissionService.EnsureCanReadCountingCircle(voteResult.CountingCircleId, voteResult.Vote.ContestId);

        voteResult.Results = voteResult.Results
            .OrderBy(r => r.Ballot.Position)
            .ToList();

        foreach (var result in voteResult.Results)
        {
            result.OrderQuestionResultsAndSubTotals();
        }

        return voteResult;
    }
}
