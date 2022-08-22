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

public class ProportionalElectionResultBundleProcessor :
    IEventProcessor<ProportionalElectionResultBundleCreated>,
    IEventProcessor<ProportionalElectionResultBundleDeleted>,
    IEventProcessor<ProportionalElectionResultBundleReviewSucceeded>,
    IEventProcessor<ProportionalElectionResultBundleReviewRejected>,
    IEventProcessor<ProportionalElectionResultBallotCreated>,
    IEventProcessor<ProportionalElectionResultBallotUpdated>,
    IEventProcessor<ProportionalElectionResultBallotDeleted>,
    IEventProcessor<ProportionalElectionResultBundleSubmissionFinished>,
    IEventProcessor<ProportionalElectionResultBundleCorrectionFinished>
{
    private readonly IDbRepository<DataContext, ProportionalElectionResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _resultRepo;
    private readonly ProportionalElectionResultBallotBuilder _ballotBuilder;
    private readonly ProportionalElectionResultBuilder _resultBuilder;
    private readonly MessageProducerBuffer _bundleChangedMessageProducer;

    public ProportionalElectionResultBundleProcessor(
        IDbRepository<DataContext, ProportionalElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, ProportionalElectionResultBallot> ballotRepo,
        IDbRepository<DataContext, ProportionalElectionResult> resultRepo,
        ProportionalElectionResultBallotBuilder ballotBuilder,
        ProportionalElectionResultBuilder resultBuilder,
        MessageProducerBuffer bundleChangedMessageProducer)
    {
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _resultRepo = resultRepo;
        _ballotBuilder = ballotBuilder;
        _resultBuilder = resultBuilder;
        _bundleChangedMessageProducer = bundleChangedMessageProducer;
    }

    public async Task Process(ProportionalElectionResultBundleCreated eventData)
    {
        var bundle = new ProportionalElectionResultBundle
        {
            Id = GuidParser.Parse(eventData.BundleId),
            ElectionResultId = GuidParser.Parse(eventData.ElectionResultId),
            ListId = GuidParser.ParseNullable(eventData.ListId),
            Number = eventData.BundleNumber,
            CreatedBy = eventData.EventInfo.User.ToDataUser(),
            State = BallotBundleState.InProcess,
        };
        await _bundleRepo.Create(bundle);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, 1);
        PublishBundleChangeMessage(bundle);
    }

    public async Task Process(ProportionalElectionResultBallotCreated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.CreateBallot(
            bundleId,
            eventData.BallotNumber,
            eventData.EmptyVoteCount,
            eventData.Candidates);
        await UpdateCountOfBallots(bundleId, 1);
    }

    public async Task Process(ProportionalElectionResultBallotUpdated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.UpdateBallot(
            bundleId,
            eventData.BallotNumber,
            eventData.EmptyVoteCount,
            eventData.Candidates);
    }

    public async Task Process(ProportionalElectionResultBallotDeleted eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var ballot = await _ballotRepo
                         .Query()
                         .FirstOrDefaultAsync(x => x.Number == eventData.BallotNumber && x.BundleId == bundleId)
                     ?? throw new EntityNotFoundException(new { bundleId, eventData.BallotNumber });
        await _ballotRepo.DeleteByKey(ballot.Id);
        await UpdateCountOfBallots(bundleId, -1);
    }

    public Task Process(ProportionalElectionResultBundleSubmissionFinished eventData)
        => ProcessBundleToReadyForReview(eventData.BundleId, eventData.SampleBallotNumbers);

    public Task Process(ProportionalElectionResultBundleCorrectionFinished eventData)
        => ProcessBundleToReadyForReview(eventData.BundleId, eventData.SampleBallotNumbers);

    public async Task Process(ProportionalElectionResultBundleDeleted eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);

        if (bundle.State != BallotBundleState.Reviewed)
        {
            await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, -1);
        }
        else
        {
            await RemoveVotesFromResults(bundle);
        }

        await UpdateBundleState(bundle, BallotBundleState.Deleted);
    }

    public async Task Process(ProportionalElectionResultBundleReviewRejected eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        await UpdateBundleState(bundle, BallotBundleState.InCorrection, user);
    }

    public async Task Process(ProportionalElectionResultBundleReviewSucceeded eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        await AddVotesToResults(bundle);
        await UpdateBundleState(bundle, BallotBundleState.Reviewed, user);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, -1);
    }

    private async Task AddVotesToResults(ProportionalElectionResultBundle bundle)
    {
        await _resultBuilder.AddVotesFromBundle(bundle);
        await UpdateTotalCountOfBallots(bundle, 1);
    }

    private async Task RemoveVotesFromResults(ProportionalElectionResultBundle bundle)
    {
        await _resultBuilder.RemoveVotesFromBundle(bundle);
        await UpdateTotalCountOfBallots(bundle, -1);
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
        ProportionalElectionResultBundle bundle,
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

    private async Task UpdateCountOfBundlesNotReviewedOrDeleted(Guid electionResultId, int delta)
    {
        var result = await _resultRepo.GetByKey(electionResultId)
                     ?? throw new EntityNotFoundException(electionResultId);
        result.CountOfBundlesNotReviewedOrDeleted += delta;
        if (result.CountOfBundlesNotReviewedOrDeleted < 0)
        {
            throw new ValidationException("Count of bundles not reviewed or deleted cannot be negative");
        }

        await _resultRepo.Update(result);
    }

    private async Task UpdateTotalCountOfBallots(ProportionalElectionResultBundle bundle, int factor)
    {
        var result = await _resultRepo.GetByKey(bundle.ElectionResultId)
                     ?? throw new EntityNotFoundException(bundle.ElectionResultId);
        if (bundle.ListId.HasValue)
        {
            result.ConventionalSubTotal.TotalCountOfModifiedLists += bundle.CountOfBallots * factor;
        }
        else
        {
            result.ConventionalSubTotal.TotalCountOfListsWithoutParty += bundle.CountOfBallots * factor;
        }

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

    private void PublishBundleChangeMessage(ProportionalElectionResultBundle bundle)
        => _bundleChangedMessageProducer.Add(new ProportionalElectionBundleChanged(
            bundle.Id,
            bundle.ElectionResultId));
}
