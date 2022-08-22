// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

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
    private readonly MessageProducerBuffer _bundleChangedMessageProducer;
    private readonly DataContext _dataContext;

    public VoteResultBundleProcessor(
        IDbRepository<DataContext, BallotResult> resultRepo,
        IDbRepository<DataContext, VoteResultBundle> bundleRepo,
        IDbRepository<DataContext, VoteResultBallot> ballotRepo,
        IDbRepository<DataContext, VoteResultBallotQuestionAnswer> questionBallotAnswerRepo,
        IDbRepository<DataContext, VoteResultBallotTieBreakQuestionAnswer> tieBreakQuestionBallotAnswerRepo,
        VoteResultBallotBuilder ballotBuilder,
        MessageProducerBuffer bundleChangedMessageProducer,
        DataContext dataContext)
    {
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _questionBallotAnswerRepo = questionBallotAnswerRepo;
        _tieBreakQuestionBallotAnswerRepo = tieBreakQuestionBallotAnswerRepo;
        _ballotBuilder = ballotBuilder;
        _bundleChangedMessageProducer = bundleChangedMessageProducer;
        _dataContext = dataContext;
    }

    public async Task Process(VoteResultBundleCreated eventData)
    {
        var bundle = new VoteResultBundle
        {
            Id = GuidParser.Parse(eventData.BundleId),
            BallotResultId = GuidParser.Parse(eventData.BallotResultId),
            Number = eventData.BundleNumber,
            CreatedBy = eventData.EventInfo.User.ToDataUser(),
            State = BallotBundleState.InProcess,
        };
        await _bundleRepo.Create(bundle);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.BallotResultId, 1);
        PublishBundleChangeMessage(bundle);
    }

    public async Task Process(VoteResultBallotCreated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.CreateBallot(bundleId, eventData);
        await UpdateCountOfBallots(bundleId, 1);
    }

    public async Task Process(VoteResultBallotUpdated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.UpdateBallot(bundleId, eventData);
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
    }

    public Task Process(VoteResultBundleSubmissionFinished eventData)
        => ProcessBundleToReadyForReview(eventData.BundleId, eventData.SampleBallotNumbers);

    public Task Process(VoteResultBundleCorrectionFinished eventData)
        => ProcessBundleToReadyForReview(eventData.BundleId, eventData.SampleBallotNumbers);

    public async Task Process(VoteResultBundleDeleted eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
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

        await UpdateBundleState(bundle, BallotBundleState.Deleted);
    }

    public async Task Process(VoteResultBundleReviewRejected eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        await UpdateBundleState(bundle, BallotBundleState.InCorrection, user);
    }

    public async Task Process(VoteResultBundleReviewSucceeded eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        await AddVotesToResults(bundle);
        await UpdateBundleState(bundle, BallotBundleState.Reviewed, user);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.BallotResultId, -1);
    }

    private async Task UpdateCountOfBallots(Guid bundleId, int delta)
    {
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        bundle.CountOfBallots += delta;
        await _bundleRepo.Update(bundle);
        PublishBundleChangeMessage(bundle);
    }

    private async Task UpdateBundleState(
        VoteResultBundle bundle,
        BallotBundleState newState,
        User? reviewer = null)
    {
        bundle.State = newState;
        if (reviewer != null)
        {
            bundle.ReviewedBy = reviewer;
        }

        await _bundleRepo.Update(bundle);
        PublishBundleChangeMessage(bundle);
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
        var result = await _resultRepo.GetByKey(ballotResultId)
                     ?? throw new EntityNotFoundException(ballotResultId);
        result.CountOfBundlesNotReviewedOrDeleted += delta;
        if (result.CountOfBundlesNotReviewedOrDeleted < 0)
        {
            throw new ValidationException("Count of bundles not reviewed or deleted cannot be negative");
        }

        await _resultRepo.Update(result);
    }

    private async Task UpdateTotalCountOfBallots(VoteResultBundle bundle, int factor)
    {
        var result = await _resultRepo.GetByKey(bundle.BallotResultId)
                     ?? throw new EntityNotFoundException(bundle.BallotResultId);
        result.ConventionalCountOfDetailedEnteredBallots += bundle.CountOfBallots * factor;
        await _resultRepo.Update(result);
    }

    private async Task ProcessBundleToReadyForReview(string bundleId, IList<int> sampleBallotNumbers)
    {
        var bundleGuid = GuidParser.Parse(bundleId);
        var bundle = await _bundleRepo.GetByKey(bundleGuid)
                     ?? throw new EntityNotFoundException(bundleGuid);
        await UpdateBundleState(bundle, BallotBundleState.ReadyForReview);

        if (sampleBallotNumbers.Count == 0)
        {
            return;
        }

        var ballots = await _ballotRepo.Query()
            .Where(b =>
                b.BundleId == bundleGuid
                && (sampleBallotNumbers.Contains(b.Number) || b.MarkedForReview))
            .ToListAsync();
        foreach (var ballot in ballots)
        {
            ballot.MarkedForReview = sampleBallotNumbers.Contains(ballot.Number);
        }

        await _ballotRepo.UpdateRange(ballots);
        PublishBundleChangeMessage(bundle);
    }

    private void PublishBundleChangeMessage(VoteResultBundle bundle)
        => _bundleChangedMessageProducer.Add(new VoteBundleChanged(
            bundle.Id,
            bundle.BallotResultId));

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
