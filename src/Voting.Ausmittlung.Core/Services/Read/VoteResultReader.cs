// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class VoteResultReader
{
    private readonly VoteResultRepo _repo;
    private readonly PermissionService _permissionService;

    public VoteResultReader(
        VoteResultRepo repo,
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
        var voteResult = await _repo.QueryVoteResult()
            .Where(predicate)
            .FirstOrDefaultAsync();

        if (voteResult == null)
        {
            return null;
        }

        await _permissionService.EnsureCanReadCountingCircle(voteResult.CountingCircleId, voteResult.Vote.ContestId);
        return voteResult;
    }
}
