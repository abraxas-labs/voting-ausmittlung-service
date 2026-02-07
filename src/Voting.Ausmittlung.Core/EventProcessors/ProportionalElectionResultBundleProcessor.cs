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

public class ProportionalElectionResultBundleProcessor :
    IEventProcessor<ProportionalElectionResultBundleCreated>,
    IEventProcessor<ProportionalElectionResultBundleDeleted>,
    IEventProcessor<ProportionalElectionResultBundleReviewSucceeded>,
    IEventProcessor<ProportionalElectionResultBundleReviewRejected>,
    IEventProcessor<ProportionalElectionResultBallotCreated>,
    IEventProcessor<ProportionalElectionResultBallotUpdated>,
    IEventProcessor<ProportionalElectionResultBallotDeleted>,
    IEventProcessor<ProportionalElectionResultBundleSubmissionFinished>,
    IEventProcessor<ProportionalElectionResultBundleCorrectionFinished>,
    IEventProcessor<ProportionalElectionResultBundleResetToSubmissionFinished>
{
    private readonly IDbRepository<DataContext, ProportionalElectionResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepo;
    private readonly ProportionalElectionResultBallotBuilder _ballotBuilder;
    private readonly ProportionalElectionResultBuilder _resultBuilder;
    private readonly EventLogger _eventLogger;
    private readonly ILogger<ProportionalElectionResultBundleProcessor> _logger;

    public ProportionalElectionResultBundleProcessor(
        IDbRepository<DataContext, ProportionalElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, ProportionalElectionResultBallot> ballotRepo,
        IDbRepository<DataContext, ProportionalElectionResult> resultRepo,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        ProportionalElectionResultBallotBuilder ballotBuilder,
        ProportionalElectionResultBuilder resultBuilder,
        EventLogger eventLogger,
        ILogger<ProportionalElectionResultBundleProcessor> logger)
    {
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _resultRepo = resultRepo;
        _protocolExportRepo = protocolExportRepo;
        _ballotBuilder = ballotBuilder;
        _resultBuilder = resultBuilder;
        _eventLogger = eventLogger;
        _logger = logger;
    }

    public async Task Process(ProportionalElectionResultBundleCreated eventData)
    {
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var state = BallotBundleState.InProcess;
        var log = new ProportionalElectionResultBundleLog { User = user, Timestamp = timestamp, State = state };
        var bundle = new ProportionalElectionResultBundle
        {
            Id = GuidParser.Parse(eventData.BundleId),
            ElectionResultId = GuidParser.Parse(eventData.ElectionResultId),
            ListId = GuidParser.ParseNullable(eventData.ListId),
            Number = eventData.BundleNumber,
            CreatedBy = user,
            State = state,
            Logs = [log],
        };
        await _bundleRepo.Create(bundle);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, 1);
        _eventLogger.LogBundleEvent(eventData, bundle.Id, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(ProportionalElectionResultBallotCreated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var bundleExists = await UpdateCountOfBallots(bundleId, 1);

        // A bundle may not exist in the read model, if someone triggered a "ProportionalElectionResultEntryDefined"
        // event (which deletes all bundles in the read model, but the aggregates still exist),
        // between a bundle create and a ballot create event.
        // Thats why we just log and skip the processing of this event, if the bundle does not exist.
        if (!bundleExists)
        {
            _logger.LogWarning(
                "Could not process {EventName} with ballot number {BallotNumber} because the bundle {BundleId} does not exist. Skip processing",
                nameof(ProportionalElectionResultBallotCreated),
                eventData.BallotNumber,
                bundleId);
            return;
        }

        await _ballotBuilder.CreateBallot(bundleId, eventData);
        _eventLogger.LogBundleEvent(eventData, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId));
    }

    public async Task Process(ProportionalElectionResultBallotUpdated eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        await _ballotBuilder.UpdateBallot(bundleId, eventData);
        _eventLogger.LogBundleEvent(eventData, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId));
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
        _eventLogger.LogBundleEvent(eventData, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId));
    }

    public async Task Process(ProportionalElectionResultBundleSubmissionFinished eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var log = await ProcessBundleToReadyForReview(bundleId, eventData.SampleBallotNumbers, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(ProportionalElectionResultBundleCorrectionFinished eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var log = await ProcessBundleToReadyForReview(bundleId, eventData.SampleBallotNumbers, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(ProportionalElectionResultBundleDeleted eventData)
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
        _eventLogger.LogBundleEvent(eventData, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(ProportionalElectionResultBundleReviewRejected eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var log = await UpdateBundleState(bundle, BallotBundleState.InCorrection, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(ProportionalElectionResultBundleReviewSucceeded eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var bundle = await _bundleRepo.GetByKey(bundleId)
                     ?? throw new EntityNotFoundException(bundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        await AddVotesToResults(bundle);
        var log = await UpdateBundleState(bundle, BallotBundleState.Reviewed, user, timestamp);
        await UpdateCountOfBundlesNotReviewedOrDeleted(bundle.ElectionResultId, -1);
        _eventLogger.LogBundleEvent(eventData, bundleId, politicalBusinessResultId: GuidParser.ParseNullable(eventData.ElectionResultId), log);
    }

    public async Task Process(ProportionalElectionResultBundleResetToSubmissionFinished eventData)
    {
        var bundleId = GuidParser.Parse(eventData.BundleId);
        var user = eventData.EventInfo.User.ToDataUser();
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);

        var bundle = await _bundleRepo.GetByKey(bundleId)
            ?? throw new EntityNotFoundException(bundleId);
        await UpdateCountOfBundlesNotReviewedOrDeleted(electionResultId, 1);
        await RemoveVotesFromResults(bundle);

        var log = await ProcessBundleToReadyForReview(bundleId, eventData.SampleBallotNumbers, user, timestamp);
        _eventLogger.LogBundleEvent(eventData, bundleId, electionResultId, log);
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

    private async Task<bool> UpdateCountOfBallots(Guid bundleId, int delta)
    {
        var affected = await _bundleRepo.Query()
            .Where(x => x.Id == bundleId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CountOfBallots, x => x.CountOfBallots + delta));
        return affected == 1;
    }

    private async Task<ProportionalElectionResultBundleLog> UpdateBundleState(
        ProportionalElectionResultBundle bundle,
        BallotBundleState newState,
        User user,
        DateTime timestamp)
    {
        var oldState = bundle.State;
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

        if (newState == BallotBundleState.ReadyForReview && oldState == BallotBundleState.InCorrection)
        {
            await _protocolExportRepo.Query()
                .Where(x => x.PoliticalBusinessResultBundleId == bundle.Id)
                .ExecuteDeleteAsync();

            // Whoever corrected the bundle should be the new creator
            bundle.CreatedBy = new User
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                SecureConnectId = user.SecureConnectId,
            };
        }

        var log = new ProportionalElectionResultBundleLog { User = user, Timestamp = timestamp, State = newState };
        bundle.Logs.Add(log);

        await _bundleRepo.Update(bundle);
        return log;
    }

    private async Task UpdateCountOfBundlesNotReviewedOrDeleted(Guid electionResultId, int delta)
    {
        var affected = await _resultRepo.Query()
            .Where(x => x.Id == electionResultId && x.CountOfBundlesNotReviewedOrDeleted + delta >= 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                x => x.CountOfBundlesNotReviewedOrDeleted,
                x => x.CountOfBundlesNotReviewedOrDeleted + delta));

        EntityNotFoundException.ThrowIfNoRowsAffected(affected, electionResultId);
    }

    private async Task UpdateTotalCountOfBallots(ProportionalElectionResultBundle bundle, int factor)
    {
        var delta = bundle.CountOfBallots * factor;
        var query = _resultRepo.Query()
            .Where(x => x.Id == bundle.ElectionResultId);

        var affected = bundle.ListId.HasValue
            ? await query.ExecuteUpdateAsync(setter => setter.SetProperty(
                x => x.ConventionalSubTotal.TotalCountOfModifiedLists,
                x => x.ConventionalSubTotal.TotalCountOfModifiedLists + delta))
            : await query.ExecuteUpdateAsync(setter => setter.SetProperty(
                x => x.ConventionalSubTotal.TotalCountOfListsWithoutParty,
                x => x.ConventionalSubTotal.TotalCountOfListsWithoutParty + delta));

        EntityNotFoundException.ThrowIfNoRowsAffected(affected, bundle.ElectionResultId);
    }

    private async Task<ProportionalElectionResultBundleLog> ProcessBundleToReadyForReview(Guid bundleId, IList<int> sampleBallotNumbers, User user, DateTime timestamp)
    {
        var bundle = await _bundleRepo.GetByKey(bundleId)
            ?? throw new EntityNotFoundException(bundleId);
        var log = await UpdateBundleState(bundle, BallotBundleState.ReadyForReview, user, timestamp);

        await _ballotRepo.Query()
            .Where(b => b.BundleId == bundleId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                x => x.MarkedForReview,
                x => sampleBallotNumbers.Contains(x.Number)));
        return log;
    }
}
