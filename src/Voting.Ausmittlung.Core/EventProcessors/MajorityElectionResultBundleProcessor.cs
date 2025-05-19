// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

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
    private readonly EventLogger _eventLogger;
    private readonly ILogger<MajorityElectionResultBundleProcessor> _logger;

    public MajorityElectionResultBundleProcessor(
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, MajorityElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, MajorityElectionResultBallot> ballotRepo,
        MajorityElectionResultBallotBuilder ballotBuilder,
        MajorityElectionCandidateResultBuilder candidateResultBuilder,
        MajorityElectionResultBuilder resultBuilder,
        EventLogger eventLogger,
        ILogger<MajorityElectionResultBundleProcessor> logger)
    {
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _ballotBuilder = ballotBuilder;
        _candidateResultBuilder = candidateResultBuilder;
        _resultBuilder = resultBuilder;
        _eventLogger = eventLogger;
        _logger = logger;
    }

    public async Task Process(MajorityElectionResultBundleCreated eventData)
    {
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var state = BallotBundleState.InProcess;
        var log = new MajorityElectionResultBundleLog { User = user, Timestamp = timestamp, State = state };
        var bundle = new MajorityElectionResultBundle
        {
            Id = GuidParser.Parse(eventData.BundleId),
            ElectionResultId = GuidParser.Parse(eventData.ElectionResultId),
            Number = eventData.BundleNumber,
            CreatedBy = user,
            State = state,
            Logs = [log],
        };
        await _bundleRepo.Create(bundle);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, 1);
        _eventLogger.LogBundleEvent(eventData, bundle.Id, GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(MajorityElectionResultBallotCreated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);

        // A bundle may not exist in the read model, if someone triggered a "MajorityElectionResultEntryDefined"
        // event (which deletes all bundles in the read model, but the aggregates still exist),
        // between a bundle create and a ballot create event.
        // Thats why we just log and skip the processing of this event, if the bundle does not exist.
        if (!await _ballotBuilder.CreateBallot(bundleId, eventData))
        {
            _logger.LogWarning(
                "Could not process {EventName} with ballot number {BallotNumber} because the bundle {BundleId} does not exist. Skip processing",
                nameof(MajorityElectionResultBallotCreated),
                eventData.BallotNumber,
                bundleId);
            return;
        }

        await UpdateCountOfBallots(bundleId, 1);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.ElectionResultId));
    }

    public async Task Process(MajorityElectionResultBallotUpdated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.UpdateBallot(bundleId, eventData);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.ElectionResultId));
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
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.ElectionResultId));
    }

    public async Task Process(MajorityElectionResultBundleSubmissionFinished eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var log = await ProcessBundleToReadyForReview(bundleId, eventData.SampleBallotNumbers, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(MajorityElectionResultBundleCorrectionFinished eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var log = await ProcessBundleToReadyForReview(bundleId, eventData.SampleBallotNumbers, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(MajorityElectionResultBundleDeleted eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
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

        var log = await UpdateBundleState(bundle, BallotBundleState.Deleted, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(MajorityElectionResultBundleReviewRejected eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        var log = await UpdateBundleState(bundle, BallotBundleState.InCorrection, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(MajorityElectionResultBundleReviewSucceeded eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        await AddVotesToResults(bundle);
        var log = await UpdateBundleState(bundle, BallotBundleState.Reviewed, user, timestamp);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, -1);
        _eventLogger.LogBundleEvent(eventData, bundleId, GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    private async Task UpdateCountOfBallots(Guid bundleId, int delta)
    {
        await _bundleRepo.Query()
            .Where(x => x.Id == bundleId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CountOfBallots, x => x.CountOfBallots + delta));
    }

    private async Task<MajorityElectionResultBundleLog> UpdateBundleState(
        MajorityElectionResultBundle bundle,
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

        var log = new MajorityElectionResultBundleLog { User = user, Timestamp = timestamp, State = newState };
        bundle.Logs.Add(log);

        await _bundleRepo.Update(bundle);
        return log;
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
        await _resultRepo.Query()
            .Where(x => x.Id == electionResultId)
            .ExecuteUpdateAsync(x => x.SetProperty(
                y => y.CountOfBundlesNotReviewedOrDeleted,
                y => y.CountOfBundlesNotReviewedOrDeleted + delta));
    }

    private async Task UpdateConventionalCountOfBallots(MajorityElectionResultBundle bundle, int factor)
    {
        await _resultRepo.Query()
            .Where(x => x.Id == bundle.ElectionResultId)
            .ExecuteUpdateAsync(x => x.SetProperty(
                y => y.ConventionalCountOfDetailedEnteredBallots,
                y => y.ConventionalCountOfDetailedEnteredBallots + (bundle.CountOfBallots * factor)));
    }

    private async Task<MajorityElectionResultBundleLog> ProcessBundleToReadyForReview(Guid bundleId, IList<int> sampleBallotNumbers, User user, DateTime timestamp)
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
}
