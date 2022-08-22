// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public class VoteResultImportWriter : PoliticalBusinessResultImportWriter<VoteResultAggregate, VoteResult>
{
    private readonly IDbRepository<DataContext, Vote> _voteRepo;

    public VoteResultImportWriter(IDbRepository<DataContext, Vote> voteRepo, IAggregateRepository aggregateRepository)
        : base(aggregateRepository)
    {
        _voteRepo = voteRepo;
    }

    internal async IAsyncEnumerable<VoteResultImport> BuildImports(
        Guid contestId,
        IReadOnlyCollection<EVotingVoteResult> results)
    {
        var voteIds = results.Select(x => x.PoliticalBusinessId).ToHashSet();
        var votes = await _voteRepo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == contestId && voteIds.Contains(x.Id))
            .Include(x => x.Ballots).ThenInclude(x => x.BallotQuestions)
            .Include(x => x.Ballots).ThenInclude(x => x.TieBreakQuestions)
            .ToListAsync();

        var votesById = votes.ToDictionary(x => x.Id);
        var ballotsById = votesById.Values.SelectMany(x => x.Ballots).ToDictionary(x => x.Id);
        var ballotQuestionsByNumberByBallotId =
            ballotsById.Values.ToDictionary(b => b.Id, b => b.BallotQuestions.ToDictionary(q => q.Number));
        var tieBreakQuestionsByNumberByBallotId =
            ballotsById.Values.ToDictionary(b => b.Id, b => b.TieBreakQuestions.ToDictionary(q => q.Number));

        foreach (var result in results)
        {
            if (!votesById.ContainsKey(result.PoliticalBusinessId))
            {
                throw new EntityNotFoundException(nameof(Vote), result.PoliticalBusinessId);
            }

            yield return ProcessResult(result, ballotsById, ballotQuestionsByNumberByBallotId, tieBreakQuestionsByNumberByBallotId);
        }
    }

    protected override IQueryable<VoteResult> BuildResultsQuery(Guid contestId)
        => _voteRepo.Query()
            .Where(x => x.ContestId == contestId)
            .SelectMany(x => x.Results);

    private VoteResultImport ProcessResult(
        EVotingVoteResult result,
        IReadOnlyDictionary<Guid, Ballot> ballotsById,
        IReadOnlyDictionary<Guid, Dictionary<int, BallotQuestion>> ballotQuestionsByNumberByBallotId,
        IReadOnlyDictionary<Guid, Dictionary<int, TieBreakQuestion>> tieBreakQuestionsByNumberByBallotId)
    {
        var importResult = new VoteResultImport(result.PoliticalBusinessId, result.BasisCountingCircleId);
        foreach (var ballotResult in result.BallotResults)
        {
            var importBallotResult = GetBallotResult(importResult, ballotResult, ballotsById);
            importBallotResult.CountOfVoters = ballotResult.Ballots.Count;
            ProcessBallotQuestionResult(importBallotResult, ballotResult, ballotQuestionsByNumberByBallotId);
            ProcessTieBreakQuestionResult(importBallotResult, ballotResult, tieBreakQuestionsByNumberByBallotId);
        }

        return importResult;
    }

    private void ProcessBallotQuestionResult(
        VoteBallotResultImport importData,
        EVotingVoteBallotResult ballotResult,
        IReadOnlyDictionary<Guid, Dictionary<int, BallotQuestion>> ballotQuestionsByNumberByBallotId)
    {
        foreach (var ballot in ballotResult.Ballots)
        {
            if (!ballotQuestionsByNumberByBallotId.TryGetValue(importData.BallotId, out var questionsByNumber))
            {
                throw new EntityNotFoundException(nameof(Ballot), importData.BallotId);
            }

            foreach (var questionAnswer in ballot.QuestionAnswers)
            {
                var questionNumber = questionAnswer.QuestionNumber;
                if (!questionsByNumber.ContainsKey(questionNumber))
                {
                    throw new EntityNotFoundException(nameof(BallotQuestion), questionNumber);
                }

                var questionResult = importData.GetOrAddQuestionResult(questionNumber);
                UpdateBallotQuestionResultAnswerCount(questionResult, questionAnswer.Answer);
            }
        }
    }

    private void ProcessTieBreakQuestionResult(
        VoteBallotResultImport importData,
        EVotingVoteBallotResult ballotResult,
        IReadOnlyDictionary<Guid, Dictionary<int, TieBreakQuestion>> tieBreakQuestionsByNumberByBallotId)
    {
        foreach (var ballot in ballotResult.Ballots)
        {
            if (!tieBreakQuestionsByNumberByBallotId.TryGetValue(importData.BallotId, out var questionsByNumber))
            {
                throw new EntityNotFoundException(nameof(Ballot), importData.BallotId);
            }

            foreach (var questionAnswer in ballot.TieBreakQuestionAnswers)
            {
                var questionNumber = questionAnswer.QuestionNumber;
                if (!questionsByNumber.ContainsKey(questionNumber))
                {
                    throw new EntityNotFoundException(nameof(TieBreakQuestion), questionNumber);
                }

                var questionResult = importData.GetOrAddTieBreakQuestionResult(questionNumber);
                UpdateTieBreakQuestionResultAnswerCount(questionResult, questionAnswer.Answer);
            }
        }
    }

    private VoteBallotResultImport GetBallotResult(
        VoteResultImport importData,
        EVotingVoteBallotResult ballotResult,
        IReadOnlyDictionary<Guid, Ballot> ballotsById)
    {
        if (!ballotsById.TryGetValue(ballotResult.BallotId, out var ballot) ||
            ballot.VoteId != importData.VoteId)
        {
            throw new EntityNotFoundException(nameof(Ballot), ballotResult.BallotId);
        }

        return importData.GetOrAddBallotResult(ballotResult.BallotId);
    }

    private void UpdateBallotQuestionResultAnswerCount(BallotQuestionResultImport questionResult, BallotQuestionAnswer answer)
    {
        switch (answer)
        {
            case BallotQuestionAnswer.Yes:
                questionResult.CountYes++;
                break;
            case BallotQuestionAnswer.No:
                questionResult.CountNo++;
                break;
            case BallotQuestionAnswer.Unspecified:
                questionResult.CountUnspecified++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(answer), answer, null);
        }
    }

    private void UpdateTieBreakQuestionResultAnswerCount(TieBreakQuestionResultImport questionResult, TieBreakQuestionAnswer answer)
    {
        switch (answer)
        {
            case TieBreakQuestionAnswer.Q1:
                questionResult.CountQ1++;
                break;
            case TieBreakQuestionAnswer.Q2:
                questionResult.CountQ2++;
                break;
            case TieBreakQuestionAnswer.Unspecified:
                questionResult.CountUnspecified++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(answer), answer, null);
        }
    }
}
