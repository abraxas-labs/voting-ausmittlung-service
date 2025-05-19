// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class VoteResultProcessor :
    PoliticalBusinessResultProcessor<VoteResult>,
    IEventProcessor<VoteResultSubmissionStarted>,
    IEventProcessor<VoteResultEntryDefined>,
    IEventProcessor<VoteResultEntered>,
    IEventProcessor<VoteResultCorrectionEntered>,
    IEventProcessor<VoteResultSubmissionFinished>,
    IEventProcessor<VoteResultCorrectionFinished>,
    IEventProcessor<VoteResultFlaggedForCorrection>,
    IEventProcessor<VoteResultAuditedTentatively>,
    IEventProcessor<VoteResultPlausibilised>,
    IEventProcessor<VoteResultResettedToSubmissionFinished>,
    IEventProcessor<VoteResultResettedToAuditedTentatively>,
    IEventProcessor<VoteResultCountOfVotersEntered>,
    IEventProcessor<VoteResultResetted>,
    IEventProcessor<VoteResultPublished>,
    IEventProcessor<VoteResultUnpublished>
{
    private readonly VoteEndResultBuilder _endResultBuilder;
    private readonly EventLogger _eventLogger;
    private readonly VoteResultBuilder _resultBuilder;

    public VoteResultProcessor(
        EventLogger eventLogger,
        VoteResultRepo voteResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        IDbRepository<DataContext, CountingCircleResultComment> commentRepo,
        VoteEndResultBuilder endResultBuilder,
        VoteResultBuilder resultBuilder)
        : base(voteResultRepo, simpleResultRepo, commentRepo)
    {
        _eventLogger = eventLogger;
        _resultBuilder = resultBuilder;
        _endResultBuilder = endResultBuilder;
    }

    public async Task Process(VoteResultSubmissionStarted eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultEntryDefined eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await _resultBuilder.UpdateResultEntryAndResetConventionalResults(voteResultId, eventData.ResultEntry, eventData.ResultEntryParams);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultCountOfVotersEntered eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await _resultBuilder.UpdateCountOfVoters(voteResultId, eventData.ResultsCountOfVoters);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultEntered eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await _resultBuilder.UpdateResults(voteResultId, eventData.Results);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultCorrectionEntered eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await _resultBuilder.UpdateResults(voteResultId, eventData.Results);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultSubmissionFinished eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultCorrectionFinished eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        var createdComment = await CreateCommentIfNeeded(voteResultId, eventData.Comment, false, eventData.EventInfo);
        await UpdateState(voteResultId, CountingCircleResultState.CorrectionDone, eventData.EventInfo, createdComment);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultFlaggedForCorrection eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        var createdComment = await CreateCommentIfNeeded(voteResultId, eventData.Comment, true, eventData.EventInfo);
        await UpdateState(voteResultId, CountingCircleResultState.ReadyForCorrection, eventData.EventInfo, createdComment);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultAuditedTentatively eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
        await _endResultBuilder.AdjustVoteEndResult(voteResultId, false);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultPlausibilised eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.Plausibilised, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultResettedToSubmissionFinished eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        await _endResultBuilder.AdjustVoteEndResult(voteResultId, true);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultResettedToAuditedTentatively eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultResetted eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
        await _resultBuilder.ResetConventionalResultInTestingPhase(voteResultId);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultPublished eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdatePublished(voteResultId, true);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }

    public async Task Process(VoteResultUnpublished eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdatePublished(voteResultId, false);
        _eventLogger.LogResultEvent(eventData, voteResultId);
    }
}
