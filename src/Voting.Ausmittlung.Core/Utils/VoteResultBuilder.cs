// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Utils;

public class VoteResultBuilder
{
    private readonly IMapper _mapper;
    private readonly BallotRepo _ballotRepo;
    private readonly VoteResultRepo _voteResultRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly DataContext _dataContext;

    public VoteResultBuilder(
        IMapper mapper,
        BallotRepo ballotRepo,
        VoteResultRepo voteResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        DataContext dataContext)
    {
        _mapper = mapper;
        _ballotRepo = ballotRepo;
        _voteResultRepo = voteResultRepo;
        _simpleResultRepo = simpleResultRepo;
        _dataContext = dataContext;
    }

    internal async Task RebuildForVote(Guid voteId, Guid domainOfInfluenceId, bool testingPhaseEnded, Guid contestId)
    {
        await _voteResultRepo.Rebuild(voteId, domainOfInfluenceId, testingPhaseEnded, contestId);
        var voteResults = await _voteResultRepo.Query()
            .Where(vr => vr.VoteId == voteId)
            .Include(x => x.CountingCircle)
            .ToListAsync();

        var ballots = await _ballotRepo.GetByVoteIdWithResultsAsTracked(voteId);
        foreach (var ballot in ballots)
        {
            AddMissingResultsToBallot(ballot, voteResults);
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task RebuildForBallot(Guid ballotId)
    {
        var voteResults = await _voteResultRepo.Query()
            .Where(vr => vr.Vote.Ballots.Any(b => b.Id == ballotId))
            .Include(x => x.CountingCircle)
            .ToListAsync();

        var ballot = await _ballotRepo.GetWithResultsAsTracked(ballotId)
                     ?? throw new EntityNotFoundException(ballotId);

        AddMissingResultsToBallot(ballot, voteResults);

        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetForVote(Guid voteId, Guid domainOfInfluenceId, Guid contestId)
    {
        var existingVoteResults = await _voteResultRepo.Query()
            .Include(x => x.CountingCircle)
            .Where(vr => vr.VoteId == voteId)
            .ToListAsync();

        await _voteResultRepo.DeleteRangeByKey(existingVoteResults.Select(x => x.Id));
        await _voteResultRepo.CreateRange(existingVoteResults.Select(vr => new VoteResult
        {
            Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(voteId, vr.CountingCircle.BasisCountingCircleId, true),
            CountingCircleId = vr.CountingCircleId,
            VoteId = vr.VoteId,
        }));

        await RebuildForVote(voteId, domainOfInfluenceId, true, contestId);
    }

    internal async Task UpdateResultEntryAndResetConventionalResults(
        Guid voteResultId,
        SharedProto.VoteResultEntry resultEntry,
        VoteResultEntryParamsEventData resultEntryParams)
    {
        var voteResult = await _voteResultRepo.GetVoteResultWithQuestionResultsAsTracked(voteResultId)
            ?? throw new EntityNotFoundException(voteResultId);

        voteResult.Entry = _mapper.Map<VoteResultEntry>(resultEntry);
        if (resultEntryParams == null)
        {
            voteResult.EntryParams = null;
        }
        else
        {
            voteResult.EntryParams = new VoteResultEntryParams();
            _mapper.Map(resultEntryParams, voteResult.EntryParams);

            // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
            if (voteResult.EntryParams.ReviewProcedure == VoteReviewProcedure.Unspecified)
            {
                voteResult.EntryParams.ReviewProcedure = VoteReviewProcedure.Electronically;
            }
        }

        await ResetConventionalResult(voteResult, false);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetConventionalResultInTestingPhase(Guid voteResultId)
    {
        var voteResult = await _voteResultRepo.GetVoteResultWithQuestionResultsAsTracked(voteResultId)
                 ?? throw new EntityNotFoundException(voteResultId);

        await ResetConventionalResult(voteResult, true);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task UpdateResults(
        string resultId,
        IEnumerable<VoteBallotResultsEventData> results)
    {
        var voteResultId = GuidParser.Parse(resultId);
        var voteResult = await _voteResultRepo.GetVoteResultWithQuestionResultsAsTracked(voteResultId)
                         ?? throw new EntityNotFoundException(voteResultId);

        if (voteResult.Results.Count == 0)
        {
            // Cannot update results, when missing ballots/results
            // This can happen in the testing phase, when a vote has been created, but no ballots
            return;
        }

        UpdateResults(voteResult, results);
        await _dataContext.SaveChangesAsync();

        await UpdateSimpleResult(voteResult);
    }

    internal void UpdateResults(
        VoteResult voteResult,
        IEnumerable<VoteBallotResultsEventData> results)
    {
        var ballotById = voteResult.Results.ToDictionary(r => r.BallotId);
        foreach (var ballotResult in results)
        {
            var ballotId = GuidParser.Parse(ballotResult.BallotId);
            if (!ballotById.TryGetValue(ballotId, out var existingResult))
            {
                throw new EntityNotFoundException(ballotId);
            }

            UpdateBallotQuestionResultSubTotal(existingResult, ballotResult);
            UpdateTieBreakQuestionResultSubTotal(existingResult, ballotResult);
        }
    }

    internal async Task UpdateCountOfVoters(
        string resultId,
        IEnumerable<VoteBallotResultsCountOfVotersEventData> resultsCountOfVoters)
    {
        var voteResultId = GuidParser.Parse(resultId);
        var voteResult = await _voteResultRepo.Query()
            .Include(x => x.Results)
            .ThenInclude(x => x.Ballot)
            .FirstOrDefaultAsync(x => x.Id == voteResultId)
            ?? throw new EntityNotFoundException(nameof(VoteResult), voteResultId);

        if (voteResult.Results.Count == 0)
        {
            // Cannot update count of voters, missing ballots/results
            // This can happen in the testing phase, when a vote has been created, but no ballots
            return;
        }

        UpdateCountOfVoters(voteResult, resultsCountOfVoters);
        await _voteResultRepo.Update(voteResult);

        await UpdateSimpleResult(voteResult);
    }

    internal void UpdateCountOfVoters(
        VoteResult voteResult,
        IEnumerable<VoteBallotResultsCountOfVotersEventData> resultsCountOfVoters)
    {
        var ballotById = voteResult.Results.ToDictionary(r => r.BallotId);
        foreach (var ballotResult in resultsCountOfVoters)
        {
            var ballotId = GuidParser.Parse(ballotResult.BallotId);
            if (!ballotById.TryGetValue(ballotId, out var existingResult))
            {
                throw new EntityNotFoundException(ballotId);
            }

            _mapper.Map(ballotResult.CountOfVoters, existingResult.CountOfVoters);
        }

        voteResult.UpdateVoterParticipation();
    }

    internal void ResetConventionalBallotResult(BallotResult ballotResult)
    {
        ballotResult.CountOfBundlesNotReviewedOrDeleted = 0;
        ballotResult.ConventionalCountOfDetailedEnteredBallots = 0;
        ballotResult.Bundles.Clear();
    }

    private void AddMissingResultsToBallot(Ballot ballot, IEnumerable<VoteResult> voteResults)
    {
        AddMissingBallotResults(ballot, voteResults);

        foreach (var result in ballot.Results)
        {
            AddMissingBallotQuestionResults(result, ballot.BallotQuestions);
            AddMissingTieBreakQuestionResults(result, ballot.TieBreakQuestions);
        }
    }

    private void AddMissingBallotQuestionResults(BallotResult result, IEnumerable<BallotQuestion> questions)
    {
        var existingQuestionResultQuestionIds = result.QuestionResults.Select(qr => qr.QuestionId).ToList();
        var newBallotQuestionResults = questions.Where(b => !existingQuestionResultQuestionIds.Contains(b.Id))
            .Select(bq => new BallotQuestionResult
            {
                QuestionId = bq.Id,
                BallotResultId = result.Id,
            });
        foreach (var newBallotQuestionResult in newBallotQuestionResults)
        {
            result.QuestionResults.Add(newBallotQuestionResult);
        }
    }

    private void AddMissingTieBreakQuestionResults(BallotResult result, IEnumerable<TieBreakQuestion> questions)
    {
        var existingQuestionResultQuestionIds = result.TieBreakQuestionResults.Select(qr => qr.QuestionId).ToList();
        var newTieBreakQuestionResults = questions.Where(b => !existingQuestionResultQuestionIds.Contains(b.Id))
            .Select(bq => new TieBreakQuestionResult
            {
                QuestionId = bq.Id,
                BallotResultId = result.Id,
            });
        foreach (var newBallotQuestionResult in newTieBreakQuestionResults)
        {
            result.TieBreakQuestionResults.Add(newBallotQuestionResult);
        }
    }

    private void AddMissingBallotResults(
        Ballot ballot,
        IEnumerable<VoteResult> voteResults)
    {
        var existingVoteResultIds = ballot.Results.Select(br => br.VoteResultId).ToHashSet();
        var newBallotResults = voteResults
            .Where(x => !existingVoteResultIds.Contains(x.Id))
            .Select(vr => new BallotResult
            {
                Id = AusmittlungUuidV5.BuildVoteBallotResult(ballot.Id, vr.CountingCircle.BasisCountingCircleId),
                BallotId = ballot.Id,
                VoteResultId = vr.Id,
            });

        foreach (var newBallotResult in newBallotResults)
        {
            ballot.Results.Add(newBallotResult);

            // we need to set the added state explicitly
            // otherwise ef decides this based on the value of the primary key.
            _dataContext.Entry(newBallotResult).State = EntityState.Added;
            _dataContext.Entry(newBallotResult).Reference(x => x.CountOfVoters).TargetEntry!.State = EntityState.Added;
        }
    }

    private async Task ResetConventionalResult(VoteResult voteResult, bool includeCountOfVoters)
    {
        voteResult.ResetAllSubTotals(VotingDataSource.Conventional, includeCountOfVoters);
        foreach (var ballotResult in voteResult.Results)
        {
            ResetConventionalBallotResult(ballotResult);
        }

        await ResetSimpleResult(voteResult);
    }

    private void UpdateBallotQuestionResultSubTotal(
        BallotResult existingResult,
        VoteBallotResultsEventData updatedResults)
    {
        var questionResultsByNumber = existingResult.QuestionResults.ToDictionary(x => x.Question.Number);
        foreach (var questionResult in updatedResults.QuestionResults)
        {
            if (!questionResultsByNumber.TryGetValue(questionResult.QuestionNumber, out var existingQuestionResult))
            {
                throw new EntityNotFoundException(questionResult.QuestionNumber);
            }

            existingQuestionResult.ConventionalSubTotal.TotalCountOfAnswerYes = questionResult.ReceivedCountYes;
            existingQuestionResult.ConventionalSubTotal.TotalCountOfAnswerNo = questionResult.ReceivedCountNo;
            existingQuestionResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified = questionResult.ReceivedCountUnspecified;
        }
    }

    private void UpdateTieBreakQuestionResultSubTotal(
        BallotResult existingResult,
        VoteBallotResultsEventData updatedResults)
    {
        var questionResultsByNumber = existingResult.TieBreakQuestionResults.ToDictionary(x => x.Question.Number);
        foreach (var questionResult in updatedResults.TieBreakQuestionResults)
        {
            if (!questionResultsByNumber.TryGetValue(questionResult.QuestionNumber, out var existingQuestionResult))
            {
                throw new EntityNotFoundException(questionResult.QuestionNumber);
            }

            existingQuestionResult.ConventionalSubTotal.TotalCountOfAnswerQ1 = questionResult.ReceivedCountQ1;
            existingQuestionResult.ConventionalSubTotal.TotalCountOfAnswerQ2 = questionResult.ReceivedCountQ2;
            existingQuestionResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified = questionResult.ReceivedCountUnspecified;
        }
    }

    private async Task UpdateSimpleResult(VoteResult voteResult)
    {
        // monitoring political businesses overview just need to display the first ballot values for count of voters
        var firstBallotResult = voteResult.Results.First(x => x.Ballot.Position == 1);

        var simpleResult = await _simpleResultRepo.GetByKey(voteResult.Id)
                           ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), voteResult.Id);

        simpleResult.CountOfVoters = firstBallotResult.CountOfVoters;
        await _simpleResultRepo.Update(simpleResult);
    }

    private async Task ResetSimpleResult(VoteResult voteResult)
    {
        var simpleResult = await _simpleResultRepo.GetByKey(voteResult.Id)
                           ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), voteResult.Id);

        simpleResult.CountOfVoters.ConventionalReceivedBallots = 0;
        simpleResult.CountOfVoters.ConventionalBlankBallots = 0;
        simpleResult.CountOfVoters.ConventionalInvalidBallots = 0;
        simpleResult.CountOfVoters.ConventionalAccountedBallots = 0;

        await _simpleResultRepo.Update(simpleResult);
    }
}
