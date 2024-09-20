// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class VoteEndResultReader
{
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, VoteEndResult> _repo;
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly IDbRepository<DataContext, VoteResult> _voteResultRepo;

    public VoteEndResultReader(
        PermissionService permissionService,
        IDbRepository<DataContext, VoteEndResult> endResultRepo,
        IDbRepository<DataContext, Vote> voteRepo,
        IDbRepository<DataContext, VoteResult> voteResultRepo)
    {
        _permissionService = permissionService;
        _repo = endResultRepo;
        _voteRepo = voteRepo;
        _voteResultRepo = voteResultRepo;
    }

    public async Task<VoteEndResult> GetEndResult(Guid voteId)
    {
        var tenantId = _permissionService.TenantId;

        var voteEndResult = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.Translations)
            .Include(x => x.Vote.DomainOfInfluence)
            .Include(x => x.Vote.Contest.Translations)
            .Include(x => x.Vote.Contest.DomainOfInfluence)
            .Include(x => x.Vote.Contest.CantonDefaults)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .Include(x => x.BallotEndResults)
                .ThenInclude(x => x.Ballot)
            .Include(x => x.BallotEndResults)
                .ThenInclude(x => x.QuestionEndResults)
                    .ThenInclude(x => x.Question.Translations)
            .Include(x => x.BallotEndResults)
                .ThenInclude(x => x.TieBreakQuestionEndResults)
                    .ThenInclude(x => x.Question.Translations)
            .FirstOrDefaultAsync(v => v.VoteId == voteId && v.Vote.DomainOfInfluence.SecureConnectId == tenantId)
            ?? throw new EntityNotFoundException(voteId);

        OrderEntities(voteEndResult);
        return voteEndResult;
    }

    public async Task<VoteEndResult> GetPartialEndResult(Guid voteId)
    {
        var vote = await _voteRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Contest.DomainOfInfluence)
            .Include(x => x.Contest.Translations)
            .Include(x => x.Contest.CantonDefaults)
            .FirstOrDefaultAsync(e => e.Id == voteId)
            ?? throw new EntityNotFoundException(voteId);
        var partialResultsCountingCircleIds = await _permissionService.GetViewablePartialResultsCountingCircleIds(vote.ContestId);

        if (partialResultsCountingCircleIds.Count == 0)
        {
            throw new EntityNotFoundException(voteId);
        }

        var voteResults = await _voteResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle.ContestDetails)
            .ThenInclude(x => x.VotingCards)
            .Include(x => x.CountingCircle.ContestDetails)
            .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.Results)
            .ThenInclude(x => x.Ballot)
            .Include(x => x.Results)
            .ThenInclude(x => x.QuestionResults)
            .ThenInclude(x => x.Question.Translations)
            .Include(x => x.Results)
            .ThenInclude(x => x.TieBreakQuestionResults)
            .ThenInclude(x => x.Question.Translations)
            .Where(x => x.VoteId == voteId && partialResultsCountingCircleIds.Contains(x.CountingCircleId))
            .ToListAsync();

        if (voteResults.Count == 0)
        {
            throw new EntityNotFoundException(voteId);
        }

        var partialEndResult = PartialEndResultUtils.MergeIntoPartialEndResult(vote, voteResults);
        OrderEntities(partialEndResult);
        return partialEndResult;
    }

    private void OrderEntities(VoteEndResult voteEndResult)
    {
        voteEndResult.OrderVotingCardsAndSubTotals();
        voteEndResult.BallotEndResults = voteEndResult.BallotEndResults
            .OrderBy(x => x.Ballot.Position)
            .ToList();

        foreach (var ballotEndResult in voteEndResult.BallotEndResults)
        {
            ballotEndResult.QuestionEndResults = ballotEndResult.QuestionEndResults
                .OrderBy(qr => qr.Question.Number)
                .ToList();

            ballotEndResult.TieBreakQuestionEndResults = ballotEndResult.TieBreakQuestionEndResults
                .OrderBy(qr => qr.Question.Number)
                .ToList();
        }
    }
}
