// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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

        return MergeIntoPartialEndResult(vote, voteResults);
    }

    private VoteEndResult MergeIntoPartialEndResult(Vote vote, List<VoteResult> results)
    {
        var partialResult = new VoteEndResult
        {
            Vote = vote,
            VoteId = vote.Id,
            VotingCards = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.VotingCards))
                .GroupBy(vc => (vc.Channel, vc.Valid, vc.DomainOfInfluenceType))
                .Select(g => new VoteEndResultVotingCardDetail
                {
                    Channel = g.Key.Channel,
                    Valid = g.Key.Valid,
                    DomainOfInfluenceType = g.Key.DomainOfInfluenceType,
                    CountOfReceivedVotingCards = g.Sum(x => x.CountOfReceivedVotingCards),
                })
                .ToList(),
            CountOfVotersInformationSubTotals = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.CountOfVotersInformationSubTotals))
                .GroupBy(cov => (cov.Sex, cov.VoterType))
                .Select(g => new VoteEndResultCountOfVotersInformationSubTotal
                {
                    VoterType = g.Key.VoterType,
                    Sex = g.Key.Sex,
                    CountOfVoters = g.Sum(x => x.CountOfVoters),
                })
                .ToList(),
            TotalCountOfVoters = results.Sum(r => r.TotalCountOfVoters),
            CountOfDoneCountingCircles = results.Count(r => r.AuditedTentativelyTimestamp.HasValue),
            TotalCountOfCountingCircles = results.Count,
            BallotEndResults = results
                .SelectMany(r => r.Results)
                .GroupBy(r => r.BallotId)
                .Select(g => new BallotEndResult
                {
                    Ballot = g.First().Ballot,
                    BallotId = g.Key,
                    CountOfVoters = new PoliticalBusinessCountOfVoters
                    {
                        ConventionalAccountedBallots = g.Sum(r => r.CountOfVoters.ConventionalAccountedBallots ?? 0),
                        ConventionalBlankBallots = g.Sum(r => r.CountOfVoters.ConventionalBlankBallots ?? 0),
                        ConventionalInvalidBallots = g.Sum(r => r.CountOfVoters.ConventionalInvalidBallots ?? 0),
                        ConventionalReceivedBallots = g.Sum(r => r.CountOfVoters.ConventionalReceivedBallots ?? 0),
                        EVotingAccountedBallots = g.Sum(r => r.CountOfVoters.EVotingAccountedBallots),
                        EVotingBlankBallots = g.Sum(r => r.CountOfVoters.EVotingBlankBallots),
                        EVotingInvalidBallots = g.Sum(r => r.CountOfVoters.EVotingInvalidBallots),
                        EVotingReceivedBallots = g.Sum(r => r.CountOfVoters.EVotingReceivedBallots),
                    },
                    QuestionEndResults = g
                        .SelectMany(x => x.QuestionResults)
                        .GroupBy(x => x.QuestionId)
                        .Select(x => new BallotQuestionEndResult
                        {
                            Question = x.First().Question,
                            QuestionId = x.Key,
                            ConventionalSubTotal = new BallotQuestionResultSubTotal
                            {
                                TotalCountOfAnswerNo = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerNo ?? 0),
                                TotalCountOfAnswerYes = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerYes ?? 0),
                                TotalCountOfAnswerUnspecified = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerUnspecified ?? 0),
                            },
                            EVotingSubTotal = new BallotQuestionResultSubTotal
                            {
                                TotalCountOfAnswerNo = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerNo),
                                TotalCountOfAnswerYes = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerYes),
                                TotalCountOfAnswerUnspecified = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerUnspecified),
                            },
                            CountOfCountingCircleNo = x.Count(r => !r.HasMajority),
                            CountOfCountingCircleYes = x.Count(r => r.HasMajority),
                        })
                        .ToList(),
                    TieBreakQuestionEndResults = g
                        .SelectMany(x => x.TieBreakQuestionResults)
                        .GroupBy(x => x.QuestionId)
                        .Select(x => new TieBreakQuestionEndResult
                        {
                            Question = x.First().Question,
                            QuestionId = x.Key,
                            ConventionalSubTotal = new TieBreakQuestionResultSubTotal
                            {
                                TotalCountOfAnswerQ1 = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerQ1 ?? 0),
                                TotalCountOfAnswerQ2 = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerQ2 ?? 0),
                                TotalCountOfAnswerUnspecified = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerUnspecified ?? 0),
                            },
                            EVotingSubTotal = new TieBreakQuestionResultSubTotal
                            {
                                TotalCountOfAnswerQ1 = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerQ1),
                                TotalCountOfAnswerQ2 = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerQ2),
                                TotalCountOfAnswerUnspecified = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerUnspecified),
                            },
                            CountOfCountingCircleQ1 = x.Count(r => r.HasQ1Majority),
                            CountOfCountingCircleQ2 = x.Count(r => r.HasQ2Majority),
                        })
                        .ToList(),
                })
                .ToList(),

            // Not enough information for this, just initialize it with the default value
            Finalized = false,
        };

        foreach (var ballotPartialResult in partialResult.BallotEndResults)
        {
            ballotPartialResult.CountOfVoters.UpdateVoterParticipation(partialResult.TotalCountOfVoters);
        }

        OrderEntities(partialResult);
        return partialResult;
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
