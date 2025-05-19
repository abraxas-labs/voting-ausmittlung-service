// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class VoteResultBundleProcessor :
    IEventProcessor<VoteResultBundleCreated>,
    IEventProcessor<VoteResultBundleDeleted>,
    IEventProcessor<VoteResultBundleReviewSucceeded>,
    IEventProcessor<VoteResultBundleReviewRejected>,
    IEventProcessor<VoteResultBallotCreated>,
    IEventProcessor<VoteResultBallotUpdated>,
    IEventProcessor<VoteResultBallotDeleted>,
    IEventProcessor<VoteResultBundleSubmissionFinished>,
    IEventProcessor<VoteResultBundleCorrectionFinished>
{
    private readonly IDbRepository<DataContext, BallotResult> _resultRepo;
    private readonly IDbRepository<DataContext, VoteResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, VoteResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, VoteResultBallotQuestionAnswer> _questionBallotAnswerRepo;
    private readonly IDbRepository<DataContext, VoteResultBallotTieBreakQuestionAnswer> _tieBreakQuestionBallotAnswerRepo;
    private readonly VoteResultBallotBuilder _ballotBuilder;
    private readonly EventLogger _eventLogger;
    private readonly DataContext _dataContext;

    public VoteResultBundleProcessor(
        IDbRepository<DataContext, BallotResult> resultRepo,
        IDbRepository<DataContext, VoteResultBundle> bundleRepo,
        IDbRepository<DataContext, VoteResultBallot> ballotRepo,
        IDbRepository<DataContext, VoteResultBallotQuestionAnswer> questionBallotAnswerRepo,
        IDbRepository<DataContext, VoteResultBallotTieBreakQuestionAnswer> tieBreakQuestionBallotAnswerRepo,
        VoteResultBallotBuilder ballotBuilder,
        EventLogger eventLogger,
        DataContext dataContext)
    {
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _questionBallotAnswerRepo = questionBallotAnswerRepo;
        _tieBreakQuestionBallotAnswerRepo = tieBreakQuestionBallotAnswerRepo;
        _ballotBuilder = ballotBuilder;
        _eventLogger = eventLogger;
        _dataContext = dataContext;
    }

    public async Task Process(VoteResultBundleCreated eventData)
    {
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var state = BallotBundleState.InProcess;
        var log = new VoteResultBundleLog { User = user, Timestamp = timestamp, State = state };
        var bundle = new VoteResultBundle
        {
            Id = GuidParser.Parse(eventData.BundleId),
            BallotResultId = GuidParser.Parse(eventData.BallotResultId),
            Number = eventData.BundleNumber,
            CreatedBy = user,
            State = state,
            Logs = [log],
        };
        await _bundleRepo.Create(bundle);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.BallotResultId, 1);
        _eventLogger.LogBundleEvent(eventData, bundle.Id, GuidParser.ParseNullable(eventData.VoteResultId), log);
    }

    public async Task Process(VoteResultBallotCreated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.CreateBallot(bundleId, eventData);
        await UpdateCountOfBallots(bundleId, 1);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.VoteResultId));
    }

    public async Task Process(VoteResultBallotUpdated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.UpdateBallot(bundleId, eventData);
        _eventLogger.LogEvent(eventData, bundleId, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.VoteResultId));
    }

    public async Task Process(VoteResultBallotDeleted eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var ballot = await _ballotRepo
                         .Query()
                         .FirstOrDefaultAsync(x => x.Number == eventData.BallotNumber && x.BundleId == bundleId)
                     ?? throw new EntityNotFoundException(new { bundleId, eventData.BallotNumber });
        await _ballotRepo.DeleteByKey(ballot.Id);
        await UpdateCountOfBallots(bundleId, -1);
        _eventLogger.LogEvent(eventData, bundleId, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.VoteResultId));
    }

    public async Task Process(VoteResultBundleSubmissionFinished eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var log = await ProcessBundleToReadyForReview(bundleId, eventData.SampleBallotNumbers, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.VoteResultId), log);
    }

    public async Task Process(VoteResultBundleCorrectionFinished eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var log = await ProcessBundleToReadyForReview(bundleId, eventData.SampleBallotNumbers, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.VoteResultId), log);
    }

    public async Task Process(VoteResultBundleDeleted eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);

        if (bundle.State != BallotBundleState.Reviewed)
        {
            await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.BallotResultId, -1);
        }
        else
        {
            await RemoveVotesFromResults(bundle);
        }

        var log = await UpdateBundleState(bundle, BallotBundleState.Deleted, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.VoteResultId), log);
    }

    public async Task Process(VoteResultBundleReviewRejected eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        var log = await UpdateBundleState(bundle, BallotBundleState.InCorrection, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.VoteResultId), log);
    }

    public async Task Process(VoteResultBundleReviewSucceeded eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        await AddVotesToResults(bundle);
        var log = await UpdateBundleState(bundle, BallotBundleState.Reviewed, user, timestamp);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.BallotResultId, -1);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.VoteResultId), log);
    }

    private async Task UpdateCountOfBallots(Guid bundleId, int delta)
    {
        await _bundleRepo.Query()
            .Where(x => x.Id == bundleId)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.CountOfBallots, y => y.CountOfBallots + delta));
    }

    private async Task<VoteResultBundleLog> UpdateBundleState(
        VoteResultBundle bundle,
        BallotBundleState newState,
        User user,
        DateTime timestamp)
    {
        bundle.State = newState;
        if (newState is BallotBundleState.Reviewed or BallotBundleState.InCorrection)
        {
            // Create new user since owned entity instances cannot be used by multiple owners
            bundle.ReviewedBy = new User
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                SecureConnectId = user.SecureConnectId,
            };
        }

        var log = new VoteResultBundleLog { User = user, Timestamp = timestamp, State = newState };
        bundle.Logs.Add(log);

        await _bundleRepo.Update(bundle);
        return log;
    }

    private async Task AddVotesToResults(VoteResultBundle bundle)
    {
        await AddVotesFromBundle(bundle.BallotResultId, bundle.Id);
        await UpdateTotalCountOfBallots(bundle, 1);
    }

    private async Task RemoveVotesFromResults(VoteResultBundle bundle)
    {
        await RemoveVotesFromBundle(bundle.BallotResultId, bundle.Id);
        await UpdateTotalCountOfBallots(bundle, -1);
    }

    private async Task UpdateCountOfBundlesNotReviewedOrDeleted(Guid ballotResultId, int delta)
    {
        await _resultRepo.Query()
            .Where(x => x.Id == ballotResultId)
            .ExecuteUpdateAsync(x => x.SetProperty(
                y => y.CountOfBundlesNotReviewedOrDeleted,
                y => y.CountOfBundlesNotReviewedOrDeleted + delta));
    }

    private async Task UpdateTotalCountOfBallots(VoteResultBundle bundle, int factor)
    {
        await _resultRepo.Query()
            .Where(x => x.Id == bundle.BallotResultId)
            .ExecuteUpdateAsync(x => x.SetProperty(
                y => y.ConventionalCountOfDetailedEnteredBallots,
                y => y.ConventionalCountOfDetailedEnteredBallots + (bundle.CountOfBallots * factor)));
    }

    private async Task<VoteResultBundleLog> ProcessBundleToReadyForReview(Guid bundleId, IList<int> sampleBallotNumbers, User user, DateTime timestamp)
    {
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        var log = await UpdateBundleState(bundle, BallotBundleState.ReadyForReview, user, timestamp);

        if (sampleBallotNumbers.Count == 0)
        {
            return log;
        }

        var ballots = await _ballotRepo.Query()
            .Where(b =>
                b.BundleId == bundleId
                && (sampleBallotNumbers.Contains(b.Number) || b.MarkedForReview))
            .ToListAsync();
        foreach (var ballot in ballots)
        {
            ballot.MarkedForReview = sampleBallotNumbers.Contains(ballot.Number);
        }

        await _ballotRepo.UpdateRange(ballots);
        return log;
    }

    private async Task AddVotesFromBundle(Guid ballotResultId, Guid bundleId)
    {
        await AdjustBallotQuestionVotesFromBundle(ballotResultId, bundleId, 1);
        await AdjustBallotTieBreakQuestionVotesFromBundle(ballotResultId, bundleId, 1);
    }

    private async Task RemoveVotesFromBundle(Guid ballotResultId, Guid bundleId)
    {
        await AdjustBallotQuestionVotesFromBundle(ballotResultId, bundleId, -1);
        await AdjustBallotTieBreakQuestionVotesFromBundle(ballotResultId, bundleId, -1);
    }

    private async Task AdjustBallotQuestionVotesFromBundle(Guid ballotResultId, Guid bundleId, int deltaFactor)
    {
        var result = await _resultRepo.Query()
                         .AsTracking()
                         .Include(r => r.QuestionResults).ThenInclude(qr => qr.Question)
                         .FirstOrDefaultAsync(b => b.Id == ballotResultId)
                     ?? throw new EntityNotFoundException(ballotResultId);

        var answersByQuestion = await _questionBallotAnswerRepo.Query()
            .Where(a => a.Ballot.BundleId == bundleId)
            .GroupBy(a => a.QuestionId)
            .Select(g => new
            {
                QuestionId = g.Key,
                CountYes = g.Sum(x => x.Answer == BallotQuestionAnswer.Yes ? 1 : 0),
                CountNo = g.Sum(x => x.Answer == BallotQuestionAnswer.No ? 1 : 0),
                CountUnspecified = g.Sum(x => x.Answer == BallotQuestionAnswer.Unspecified ? 1 : 0),
            })
            .ToDictionaryAsync(x => x.QuestionId, x => new
            {
                x.CountYes,
                x.CountNo,
                x.CountUnspecified,
            });

        if (answersByQuestion.Count == 0)
        {
            return;
        }

        foreach (var questionResult in result.QuestionResults)
        {
            questionResult.ConventionalSubTotal.TotalCountOfAnswerYes += answersByQuestion[questionResult.QuestionId].CountYes * deltaFactor;
            questionResult.ConventionalSubTotal.TotalCountOfAnswerNo += answersByQuestion[questionResult.QuestionId].CountNo * deltaFactor;
            questionResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified += answersByQuestion[questionResult.QuestionId].CountUnspecified * deltaFactor;
        }

        await _dataContext.SaveChangesAsync();
    }

    private async Task AdjustBallotTieBreakQuestionVotesFromBundle(Guid ballotResultId, Guid bundleId, int deltaFactor)
    {
        var result = await _resultRepo.Query()
                         .AsTracking()
                         .Include(r => r.TieBreakQuestionResults).ThenInclude(qr => qr.Question)
                         .FirstOrDefaultAsync(b => b.Id == ballotResultId)
                     ?? throw new EntityNotFoundException(ballotResultId);

        var answersByTieBreakQuestion = await _tieBreakQuestionBallotAnswerRepo.Query()
            .Where(a => a.Ballot.BundleId == bundleId)
            .GroupBy(a => a.QuestionId)
            .Select(g => new
            {
                QuestionId = g.Key,
                CountQ1 = g.Sum(x => x.Answer == TieBreakQuestionAnswer.Q1 ? 1 : 0),
                CountQ2 = g.Sum(x => x.Answer == TieBreakQuestionAnswer.Q2 ? 1 : 0),
                CountUnspecified = g.Sum(x => x.Answer == TieBreakQuestionAnswer.Unspecified ? 1 : 0),
            })
            .ToDictionaryAsync(x => x.QuestionId, x => new
            {
                x.CountQ1,
                x.CountQ2,
                x.CountUnspecified,
            });

        if (answersByTieBreakQuestion.Count == 0)
        {
            return;
        }

        foreach (var tieBreakQuestionResult in result.TieBreakQuestionResults)
        {
            tieBreakQuestionResult.ConventionalSubTotal.TotalCountOfAnswerQ1 += answersByTieBreakQuestion[tieBreakQuestionResult.QuestionId].CountQ1 * deltaFactor;
            tieBreakQuestionResult.ConventionalSubTotal.TotalCountOfAnswerQ2 += answersByTieBreakQuestion[tieBreakQuestionResult.QuestionId].CountQ2 * deltaFactor;
            tieBreakQuestionResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified += answersByTieBreakQuestion[tieBreakQuestionResult.QuestionId].CountUnspecified * deltaFactor;
        }

        await _dataContext.SaveChangesAsync();
    }
}
