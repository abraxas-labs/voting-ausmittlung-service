// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class BallotResultReader
{
    private readonly IDbRepository<DataContext, BallotResult> _repo;
    private readonly PermissionService _permissionService;

    public BallotResultReader(
        IDbRepository<DataContext, BallotResult> repo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
    }

    public async Task<BallotResult> Get(Guid ballotResultId)
    {
        var ballotResult = await _repo.Query()
                .AsSplitQuery()
                .Include(x => x.VoteResult.Vote.Translations)
                .Include(x => x.VoteResult.Vote.Contest.Translations)
                .Include(x => x.VoteResult.Vote.DomainOfInfluence)
                .Include(x => x.VoteResult.CountingCircle)
                .Include(x => x.Ballot).ThenInclude(x => x.BallotQuestions).ThenInclude(x => x.Translations)
                .Include(x => x.Ballot).ThenInclude(x => x.TieBreakQuestions).ThenInclude(x => x.Translations)
                .Include(x => x.QuestionResults).ThenInclude(qr => qr.Question.Translations)
                .Include(x => x.TieBreakQuestionResults).ThenInclude(qr => qr.Question.Translations)
                .FirstOrDefaultAsync(x => x.Id == ballotResultId)
           ?? throw new EntityNotFoundException(ballotResultId);

        await _permissionService.EnsureCanReadCountingCircle(ballotResult.VoteResult.CountingCircleId, ballotResult.VoteResult.Vote.ContestId);

        ballotResult.OrderQuestionResultsAndSubTotals();
        ballotResult.Ballot.OrderQuestions();

        return ballotResult;
    }
}
