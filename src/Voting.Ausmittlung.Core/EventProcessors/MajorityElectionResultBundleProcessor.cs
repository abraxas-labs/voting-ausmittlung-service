// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

public class MajorityElectionResultBundleProcessor
    : IEventProcessor<MajorityElectionResultBundleCreated>,
        IEventProcessor<MajorityElectionResultBundleDeleted>,
        IEventProcessor<MajorityElectionResultBundleReviewSucceeded>,
        IEventProcessor<MajorityElectionResultBundleReviewRejected>,
        IEventProcessor<MajorityElectionResultBallotCreated>,
        IEventProcessor<MajorityElectionResultBallotUpdated>,
        IEventProcessor<MajorityElectionResultBallotDeleted>,
        IEventProcessor<MajorityElectionResultBundleSubmissionFinished>,
        IEventProcessor<MajorityElectionResultBundleCorrectionFinished>
{
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBallot> _ballotRepo;
    private readonly MajorityElectionResultBallotBuilder _ballotBuilder;
    private readonly MajorityElectionCandidateResultBuilder _candidateResultBuilder;
    private readonly MajorityElectionResultBuilder _resultBuilder;
    private readonly MessageProducerBuffer _bundleChangedMessageProducer;
    private readonly ILogger<MajorityElectionResultBundleProcessor> _logger;

    public MajorityElectionResultBundleProcessor(
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, MajorityElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, MajorityElectionResultBallot> ballotRepo,
        MajorityElectionResultBallotBuilder ballotBuilder,
        MajorityElectionCandidateResultBuilder candidateResultBuilder,
        MajorityElectionResultBuilder resultBuilder,
        MessageProducerBuffer bundleChangedMessageProducer,
        ILogger<MajorityElectionResultBundleProcessor> logger)
    {
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _ballotBuilder = ballotBuilder;
        _candidateResultBuilder = candidateResultBuilder;
        _resultBuilder = resultBuilder;
        _bundleChangedMessageProducer = bundleChangedMessageProducer;
        _logger = logger;
    }

    public async Task Process(MajorityElectionResultBundleCreated eventData)
    {
        var bundle = new MajorityElectionResultBundle
        {
            Id = GuidParser.Parse(eventData.BundleId),
            ElectionResultId = GuidParser.Parse(eventData.ElectionResultId),
            Number = eventData.BundleNumber,
            CreatedBy = eventData.EventInfo.User.ToDataUser(),
            State = BallotBundleState.InProcess,
        };
        await _bundleRepo.Create(bundle);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, 1);
        PublishBundleChangeMessage(bundle);
    }

    public async Task Process(MajorityElectionResultBallotCreated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);

        // A bundle may not exist in the read model, if someone triggered a "MajorityElectionResultEntryDefined"
        // event (which deletes all bundles in the read model, but the aggregates still exist),
        // between a bundle create and a ballot create event.
        // Thats why we just log and skip the processing of this event, if the bundle does not exist.
        if (!await _bundleRepo.ExistsByKey(bundleId))
        {
            _logger.LogWarning(
                "Could not process {EventName} with ballot number {BallotNumber} because the bundle {BundleId} does not exist. Skip processing",
                nameof(MajorityElectionResultBallotCreated),
                eventData.BallotNumber,
                bundleId);
            return;
        }

        await _ballotBuilder.CreateBallot(bundleId, eventData);
        await UpdateCountOfBallots(bundleId, 1);
    }

    public async Task Process(MajorityElectionResultBallotUpdated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.UpdateBallot(bundleId, eventData);
    }

    public async Task Process(MajorityElectionResultBallotDeleted eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var ballot = await _ballotRepo
                         .Query()
                         .FirstOrDefaultAsync(x => x.Number == eventData.BallotNumber && x.BundleId == bundleId)
                     ?? throw new EntityNotFoundException(new { bundleId, eventData.BallotNumber });
        await _ballotRepo.DeleteByKey(ballot.Id);
        await UpdateCountOfBallots(bundleId, -1);
    }

    public Task Process(MajorityElectionResultBundleSubmissionFinished eventData)
        => ProcessBundleToReadyForReview(eventData.BundleId, eventData.SampleBallotNumbers);

    public Task Process(MajorityElectionResultBundleCorrectionFinished eventData)
        => ProcessBundleToReadyForReview(eventData.BundleId, eventData.SampleBallotNumbers);

    public async Task Process(MajorityElectionResultBundleDeleted eventData)
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

    public async Task Process(MajorityElectionResultBundleReviewRejected eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        await UpdateBundleState(bundle, BallotBundleState.InCorrection, user);
    }

    public async Task Process(MajorityElectionResultBundleReviewSucceeded eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        await AddVotesToResults(bundle);
        await UpdateBundleState(bundle, BallotBundleState.Reviewed, user);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, -1);
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
        MajorityElectionResultBundle bundle,
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

    private async Task AddVotesToResults(MajorityElectionResultBundle bundle)
    {
        await _candidateResultBuilder.AddConventionalVotesFromBundle(bundle.ElectionResultId, bundle.Id);
        await _resultBuilder.AddVoteCountsFromBundle(bundle.ElectionResultId, bundle.Id);
        await UpdateConventionalCountOfBallots(bundle, 1);
    }

    private async Task RemoveVotesFromResults(MajorityElectionResultBundle bundle)
    {
        await _candidateResultBuilder.RemoveConventionalVotesFromBundle(bundle.ElectionResultId, bundle.Id);
        await _resultBuilder.RemoveVoteCountsFromBundle(bundle.ElectionResultId, bundle.Id);
        await UpdateConventionalCountOfBallots(bundle, -1);
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

    private async Task UpdateConventionalCountOfBallots(MajorityElectionResultBundle bundle, int factor)
    {
        var result = await _resultRepo.GetByKey(bundle.ElectionResultId)
                     ?? throw new EntityNotFoundException(bundle.ElectionResultId);
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

    private void PublishBundleChangeMessage(MajorityElectionResultBundle bundle)
        => _bundleChangedMessageProducer.Add(new MajorityElectionBundleChanged(
            bundle.Id,
            bundle.ElectionResultId));
}
