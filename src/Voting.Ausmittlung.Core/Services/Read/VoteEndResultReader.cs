﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class VoteEndResultReader
{
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, VoteEndResult> _repo;
    private readonly IDbRepository<DataContext, ContestDomainOfInfluenceDetails> _contestDomainOfInfluenceDetailsRepo;

    public VoteEndResultReader(
        PermissionService permissionService,
        IDbRepository<DataContext, VoteEndResult> endResultRepo,
        IDbRepository<DataContext, ContestDomainOfInfluenceDetails> contestDomainOfInfluenceDetailsRepo)
    {
        _permissionService = permissionService;
        _repo = endResultRepo;
        _contestDomainOfInfluenceDetailsRepo = contestDomainOfInfluenceDetailsRepo;
    }

    public async Task<VoteEndResult> GetEndResult(Guid voteId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();
        var tenantId = _permissionService.TenantId;

        var voteEndResult = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.Translations)
            .Include(x => x.Vote.DomainOfInfluence)
            .Include(x => x.Vote.Contest.Translations)
            .Include(x => x.Vote.Contest.DomainOfInfluence)
            .Include(x => x.BallotEndResults)
                .ThenInclude(x => x.Ballot.Translations)
            .Include(x => x.BallotEndResults)
                .ThenInclude(x => x.QuestionEndResults)
                    .ThenInclude(x => x.Question.Translations)
            .Include(x => x.BallotEndResults)
                .ThenInclude(x => x.TieBreakQuestionEndResults)
                    .ThenInclude(x => x.Question.Translations)
            .FirstOrDefaultAsync(v => v.VoteId == voteId && v.Vote.DomainOfInfluence.SecureConnectId == tenantId)
            ?? throw new EntityNotFoundException(voteId);

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

        voteEndResult.Vote.DomainOfInfluence.Details = await _contestDomainOfInfluenceDetailsRepo.Query()
            .AsSplitQuery()
            .Include(d => d.CountOfVotersInformationSubTotals)
            .Include(d => d.VotingCards.Where(x => x.DomainOfInfluenceType == voteEndResult.Vote.DomainOfInfluence.Type))
            .FirstOrDefaultAsync(x => x.DomainOfInfluenceId == voteEndResult.Vote.DomainOfInfluenceId);

        voteEndResult.Vote.DomainOfInfluence.Details?.OrderVotingCardsAndSubTotals();
        return voteEndResult;
    }
}
