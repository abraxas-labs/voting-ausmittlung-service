// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class MajorityElectionResultProcessor :
    PoliticalBusinessResultProcessor<MajorityElectionResult>,
    IEventProcessor<MajorityElectionResultSubmissionStarted>,
    IEventProcessor<MajorityElectionResultEntryDefined>,
    IEventProcessor<MajorityElectionResultCountOfVotersEntered>,
    IEventProcessor<MajorityElectionCandidateResultsEntered>,
    IEventProcessor<MajorityElectionBallotGroupResultsEntered>,
    IEventProcessor<MajorityElectionResultSubmissionFinished>,
    IEventProcessor<MajorityElectionResultCorrectionFinished>,
    IEventProcessor<MajorityElectionResultFlaggedForCorrection>,
    IEventProcessor<MajorityElectionResultAuditedTentatively>,
    IEventProcessor<MajorityElectionResultPlausibilised>,
    IEventProcessor<MajorityElectionResultResettedToSubmissionFinished>,
    IEventProcessor<MajorityElectionResultResettedToAuditedTentatively>,
    IEventProcessor<MajorityElectionResultResetted>,
    IEventProcessor<MajorityElectionResultPublished>,
    IEventProcessor<MajorityElectionResultUnpublished>
{
    private readonly EventLogger _eventLogger;
    private readonly MajorityElectionResultRepo _electionResultRepo;
    private readonly MajorityElectionResultBuilder _resultBuilder;
    private readonly MajorityElectionBallotGroupResultBuilder _ballotGroupResultBuilder;
    private readonly MajorityElectionEndResultBuilder _endResultBuilder;

    public MajorityElectionResultProcessor(
        EventLogger eventLogger,
        MajorityElectionResultRepo electionResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        MajorityElectionResultBuilder resultBuilder,
        MajorityElectionBallotGroupResultBuilder ballotGroupResultBuilder,
        IDbRepository<DataContext, CountingCircleResultComment> commentRepo,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        MajorityElectionEndResultBuilder endResultBuilder,
        AggregatedContestCountingCircleDetailsBuilder aggregatedCcDetailsBuilder)
        : base(electionResultRepo, simpleResultRepo, commentRepo, protocolExportRepo, aggregatedCcDetailsBuilder)
    {
        _eventLogger = eventLogger;
        _electionResultRepo = electionResultRepo;
        _resultBuilder = resultBuilder;
        _ballotGroupResultBuilder = ballotGroupResultBuilder;
        _endResultBuilder = endResultBuilder;
    }

    public async Task Process(MajorityElectionResultSubmissionStarted eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultEntryDefined eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await _resultBuilder.UpdateResultEntryAndResetConventionalResult(electionResultId, eventData.ResultEntry, eventData.ResultEntryParams);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultCountOfVotersEntered eventData)
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

    public async Task Process(MajorityElectionCandidateResultsEntered eventData)
    {
        var resultId = GuidParser.Parse(eventData.ElectionResultId);
        await _resultBuilder.UpdateConventionalResults(resultId, eventData);
        _eventLogger.LogResultEvent(eventData, resultId);
    }

    public async Task Process(MajorityElectionBallotGroupResultsEntered eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var counts = eventData.Results.ToDictionary(x => GuidParser.Parse(x.BallotGroupId), x => x.VoteCount);
        await _ballotGroupResultBuilder.UpdateBallotGroupAndCandidateResults(electionResultId, counts);
        await _resultBuilder.UpdateTotalCountOfBallotGroupVotes(electionResultId, counts.Values.Sum());
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultSubmissionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        await _endResultBuilder.AdjustEndResult(electionResultId, false);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultCorrectionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var createdComment = await CreateCommentIfNeeded(electionResultId, eventData.Comment, false, eventData.EventInfo);
        await UpdateState(electionResultId, CountingCircleResultState.CorrectionDone, eventData.EventInfo, createdComment);
        await _endResultBuilder.AdjustEndResult(electionResultId, false);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultFlaggedForCorrection eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var createdComment = await CreateCommentIfNeeded(electionResultId, eventData.Comment, true, eventData.EventInfo);
        await UpdateState(electionResultId, CountingCircleResultState.ReadyForCorrection, eventData.EventInfo, createdComment);
        await _endResultBuilder.AdjustEndResult(electionResultId, true);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultAuditedTentatively eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultPlausibilised eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.Plausibilised, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultResettedToSubmissionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultResettedToAuditedTentatively eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultResetted eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);

        // Only the counting circle result is updated here.
        // The end result adjustments are handled by the "ContestCountingCircleDetailsResetted" event processing.
        await _resultBuilder.ResetConventionalResultInTestingPhase(electionResultId);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultPublished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdatePublished(electionResultId, true);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }

    public async Task Process(MajorityElectionResultUnpublished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdatePublished(electionResultId, false);
        _eventLogger.LogResultEvent(eventData, electionResultId);
    }
}
