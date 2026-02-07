// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionResultProcessor :
    PoliticalBusinessResultProcessor<ProportionalElectionResult>,
    IEventProcessor<ProportionalElectionResultSubmissionStarted>,
    IEventProcessor<ProportionalElectionResultEntryDefined>,
    IEventProcessor<ProportionalElectionResultCountOfVotersEntered>,
    IEventProcessor<ProportionalElectionUnmodifiedListResultsEntered>,
    IEventProcessor<ProportionalElectionResultSubmissionFinished>,
    IEventProcessor<ProportionalElectionResultCorrectionFinished>,
    IEventProcessor<ProportionalElectionResultFlaggedForCorrection>,
    IEventProcessor<ProportionalElectionResultAuditedTentatively>,
    IEventProcessor<ProportionalElectionResultPlausibilised>,
    IEventProcessor<ProportionalElectionResultResettedToSubmissionFinished>,
    IEventProcessor<ProportionalElectionResultResettedToAuditedTentatively>,
    IEventProcessor<ProportionalElectionResultResetted>,
    IEventProcessor<ProportionalElectionResultPublished>,
    IEventProcessor<ProportionalElectionResultUnpublished>
{
    private readonly EventLogger _eventLogger;
    private readonly ProportionalElectionResultRepo _electionResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnmodifiedListResult> _unmodifiedListResultRepo;
    private readonly ProportionalElectionResultBuilder _resultBuilder;
    private readonly ProportionalElectionEndResultBuilder _endResultBuilder;
    private readonly IDbRepository<DataContext, ProportionalElectionListResult> _listResultRepo;

    public ProportionalElectionResultProcessor(
        EventLogger eventLogger,
        ProportionalElectionResultRepo electionResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        IDbRepository<DataContext, ProportionalElectionUnmodifiedListResult> unmodifiedListResultRepo,
        IDbRepository<DataContext, CountingCircleResultComment> commentRepo,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        ProportionalElectionResultBuilder resultBuilder,
        ProportionalElectionEndResultBuilder endResultBuilder,
        AggregatedContestCountingCircleDetailsBuilder aggregatedCcDetailsBuilder,
        IDbRepository<DataContext, ProportionalElectionListResult> listResultRepo)
        : base(electionResultRepo, simpleResultRepo, commentRepo, protocolExportRepo, aggregatedCcDetailsBuilder)
    {
        _eventLogger = eventLogger;
        _electionResultRepo = electionResultRepo;
        _unmodifiedListResultRepo = unmodifiedListResultRepo;
        _resultBuilder = resultBuilder;
        _endResultBuilder = endResultBuilder;
        _listResultRepo = listResultRepo;
    }

    public async Task Process(ProportionalElectionResultSubmissionStarted eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultEntryDefined eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await _resultBuilder.UpdateResultEntryAndResetConventionalResults(electionResultId, eventData.ResultEntryParams);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultCountOfVotersEntered eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var electionResult = await _electionResultRepo.GetByKey(electionResultId)
                             ?? throw new EntityNotFoundException(electionResultId);

        electionResult.CountOfVoters.ConventionalSubTotal.AccountedBallots
            = eventData.CountOfVoters.ConventionalAccountedBallots;
        electionResult.CountOfVoters.ConventionalSubTotal.BlankBallots
            = eventData.CountOfVoters.ConventionalBlankBallots;
        electionResult.CountOfVoters.ConventionalSubTotal.InvalidBallots
            = eventData.CountOfVoters.ConventionalInvalidBallots;
        electionResult.CountOfVoters.ConventionalSubTotal.ReceivedBallots
            = eventData.CountOfVoters.ConventionalReceivedBallots;
        electionResult.UpdateVoterParticipation();
        await _electionResultRepo.Update(electionResult);
        await UpdateSimpleResult(electionResult.Id, electionResult.CountOfVoters);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionUnmodifiedListResultsEntered eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var electionResult = await _electionResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.UnmodifiedListResults)
            .Include(x => x.ListResults).ThenInclude(x => x.List)
            .Include(x => x.ListResults).ThenInclude(x => x.CandidateResults).ThenInclude(x => x.Candidate)
            .FirstOrDefaultAsync(x => x.Id == electionResultId)
            ?? throw new EntityNotFoundException(electionResultId);

        var unmodifiedListResultsByListId = electionResult.UnmodifiedListResults.ToDictionary(x => x.ListId);
        var listResultsByListId = electionResult.ListResults.ToDictionary(x => x.ListId);
        var unmodifiedListResultsToUpdate = new List<ProportionalElectionUnmodifiedListResult>();
        var listResultsToUpdate = new List<ProportionalElectionListResult>();
        foreach (var enteredUnmodifiedListResult in eventData.Results)
        {
            var listId = GuidParser.Parse(enteredUnmodifiedListResult.ListId);
            if (!unmodifiedListResultsByListId.TryGetValue(listId, out var unmodifiedListResult))
            {
                throw new EntityNotFoundException(listId);
            }

            if (!listResultsByListId.TryGetValue(listId, out var listResult))
            {
                throw new EntityNotFoundException(listId);
            }

            var voteCountDelta = enteredUnmodifiedListResult.VoteCount - unmodifiedListResult.ConventionalVoteCount;
            _resultBuilder.UpdateVotesFromUnmodifiedListResult(listResult, enteredUnmodifiedListResult.VoteCount, voteCountDelta);
            listResultsToUpdate.Add(listResult);

            unmodifiedListResult.ConventionalVoteCount = enteredUnmodifiedListResult.VoteCount;
            unmodifiedListResultsToUpdate.Add(unmodifiedListResult);
        }

        await _listResultRepo.UpdateRange(listResultsToUpdate);
        await _unmodifiedListResultRepo.UpdateRange(unmodifiedListResultsToUpdate);

        electionResult.ConventionalSubTotal.TotalCountOfUnmodifiedLists = electionResult.UnmodifiedListResults.Sum(x => x.ConventionalVoteCount);
        await _electionResultRepo.Update(electionResult);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultSubmissionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        await _endResultBuilder.AdjustEndResult(electionResultId, false);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultCorrectionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var createdComment = await CreateCommentIfNeeded(electionResultId, eventData.Comment, false, eventData.EventInfo);
        await UpdateState(electionResultId, CountingCircleResultState.CorrectionDone, eventData.EventInfo, createdComment);
        await _endResultBuilder.AdjustEndResult(electionResultId, false);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultFlaggedForCorrection eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var createdComment = await CreateCommentIfNeeded(electionResultId, eventData.Comment, true, eventData.EventInfo);
        await UpdateState(electionResultId, CountingCircleResultState.ReadyForCorrection, eventData.EventInfo, createdComment);
        await _endResultBuilder.AdjustEndResult(electionResultId, true);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultAuditedTentatively eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);

        // During older contests, the number of mandates were distributed implicitly when all results were audited tentatively.
        // We need to keep this behavior for backward compatibility.
        // All newer events/contests have ImplicitMandateDistributionDisabled set to true.
        if (!eventData.ImplicitMandateDistributionDisabled)
        {
            await _endResultBuilder.DistributeNumberOfMandatesImplicitly(electionResultId);
        }

        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultPlausibilised eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.Plausibilised, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultResettedToSubmissionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultResettedToAuditedTentatively eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultResetted eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);

        // Only the counting circle result is updated here.
        // The end result adjustments are handled by the "ContestCountingCircleDetailsResetted" event processing.
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
        await _resultBuilder.ResetConventionalResultInTestingPhase(electionResultId);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultPublished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdatePublished(electionResultId, true);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(ProportionalElectionResultUnpublished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdatePublished(electionResultId, false);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }
}
