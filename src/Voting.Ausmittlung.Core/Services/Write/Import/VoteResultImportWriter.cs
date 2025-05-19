// (c) Copyright by Abraxas Informatik AG
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
    private readonly IDbRepository<DataContext, Ballot> _ballotRepo;

    public VoteResultImportWriter(
        IDbRepository<DataContext, Vote> voteRepo,
        IDbRepository<DataContext, Ballot> ballotRepo,
        IAggregateRepository aggregateRepository)
        : base(aggregateRepository)
    {
        _voteRepo = voteRepo;
        _ballotRepo = ballotRepo;
    }

    internal async IAsyncEnumerable<VoteResultImport> BuildImports(
        Guid contestId,
        IReadOnlyCollection<VotingImportVoteResult> results)
    {
        // The imported votes not correlate to the votes in our system, as the "VOTING votes" are not the same as the eCH votes
        // Use the ballots instead.
        var ballotIds = results
            .SelectMany(x => x.BallotResults.Select(r => r.BallotId))
            .ToHashSet();

        var ballots = await _ballotRepo.Query()
            .AsSplitQuery()
            .Where(x => x.Vote.ContestId == contestId && ballotIds.Contains(x.Id))
            .Include(x => x.Vote)
            .Include(x => x.BallotQuestions)
            .Include(x => x.TieBreakQuestions)
            .ToListAsync();

        var ballotsById = ballots.ToDictionary(x => x.Id);
        var ballotQuestionsByNumberByBallotId =
            ballotsById.Values.ToDictionary(b => b.Id, b => b.BallotQuestions.ToDictionary(q => q.Number));
        var tieBreakQuestionsByNumberByBallotId =
            ballotsById.Values.ToDictionary(b => b.Id, b => b.TieBreakQuestions.ToDictionary(q => q.Number));

        foreach (var result in results)
        {
            var notFoundBallot = result.BallotResults.FirstOrDefault(x => !ballotsById.ContainsKey(x.BallotId));
            if (notFoundBallot != null)
            {
                throw new EntityNotFoundException(nameof(Ballot), notFoundBallot.BallotId);
            }

            // Resolve the ballots to our "VOTING votes"
            var ballotResultsByVote = result.BallotResults
                .GroupBy(x => ballotsById[x.BallotId].VoteId);

            foreach (var group in ballotResultsByVote)
            {
                var importResult = new VoteResultImport(group.Key, Guid.Parse(result.BasisCountingCircleId), result.TotalCountOfVoters);
                yield return ProcessResult(
                    importResult,
                    group,
                    ballotQuestionsByNumberByBallotId,
                    tieBreakQuestionsByNumberByBallotId);
            }
        }
    }

    protected override IQueryable<VoteResult> BuildResultsQuery(Guid contestId)
        => _voteRepo.Query()
            .Where(x => x.ContestId == contestId)
            .SelectMany(x => x.Results);

    private VoteResultImport ProcessResult(
        VoteResultImport importResult,
        IEnumerable<VotingImportVoteBallotResult> ballotResults,
        IReadOnlyDictionary<Guid, Dictionary<int, BallotQuestion>> ballotQuestionsByNumberByBallotId,
        IReadOnlyDictionary<Guid, Dictionary<int, TieBreakQuestion>> tieBreakQuestionsByNumberByBallotId)
    {
        // Enumerate through all vote ballots (="Vorlagen")
        foreach (var ballotResult in ballotResults)
        {
            var importBallotResult = importResult.GetOrAddBallotResult(ballotResult.BallotId);
            importBallotResult.CountOfVoters = ballotResult.Ballots.Count;

            if (!ballotQuestionsByNumberByBallotId.TryGetValue(importBallotResult.BallotId, out var questionsByNumber))
            {
                throw new EntityNotFoundException(nameof(Ballot), importBallotResult.BallotId);
            }

            if (!tieBreakQuestionsByNumberByBallotId.TryGetValue(importBallotResult.BallotId, out var tieBreakQuestionsByNumber))
            {
                throw new EntityNotFoundException(nameof(Ballot), importBallotResult.BallotId);
            }

            // Enumerate the ballots (="Stimmzettel")
            foreach (var ballot in ballotResult.Ballots)
            {
                // When all questions of the vote ballot have been left empty, treat the whole ballot as empty.
                if (ballot.QuestionAnswers.All(q => q.Answer == BallotQuestionAnswer.Unspecified)
                    && ballot.TieBreakQuestionAnswers.All(tq => tq.Answer == TieBreakQuestionAnswer.Unspecified))
                {
                    importBallotResult.BlankBallotCount++;
                    continue;
                }

                foreach (var questionAnswer in ballot.QuestionAnswers)
                {
                    var questionNumber = questionAnswer.QuestionNumber;
                    if (!questionsByNumber.ContainsKey(questionNumber))
                    {
                        throw new EntityNotFoundException(nameof(BallotQuestion), questionNumber);
                    }

                    var questionResult = importBallotResult.GetOrAddQuestionResult(questionNumber);
                    UpdateBallotQuestionResultAnswerCount(questionResult, questionAnswer.Answer);
                }

                foreach (var questionAnswer in ballot.TieBreakQuestionAnswers)
                {
                    var questionNumber = questionAnswer.QuestionNumber;
                    if (!tieBreakQuestionsByNumber.ContainsKey(questionNumber))
                    {
                        throw new EntityNotFoundException(nameof(TieBreakQuestion), questionNumber);
                    }

                    var questionResult = importBallotResult.GetOrAddTieBreakQuestionResult(questionNumber);
                    UpdateTieBreakQuestionResultAnswerCount(questionResult, questionAnswer.Answer);
                }
            }
        }

        return importResult;
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
