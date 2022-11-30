// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

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
    IEventProcessor<VoteResultResetted>
{
    private readonly VoteResultRepo _voteResultRepo;
    private readonly VoteEndResultBuilder _endResultBuilder;
    private readonly VoteResultBuilder _resultBuilder;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;

    public VoteResultProcessor(
        IMapper mapper,
        DataContext dataContext,
        VoteResultRepo voteResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        IDbRepository<DataContext, CountingCircleResultComment> commentRepo,
        VoteEndResultBuilder endResultBuilder,
        VoteResultBuilder resultBuilder,
        MessageProducerBuffer messageProducerBuffer)
        : base(voteResultRepo, simpleResultRepo, commentRepo, messageProducerBuffer)
    {
        _mapper = mapper;
        _voteResultRepo = voteResultRepo;
        _resultBuilder = resultBuilder;
        _endResultBuilder = endResultBuilder;
        _dataContext = dataContext;
    }

    public async Task Process(VoteResultSubmissionStarted eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
    }

    public async Task Process(VoteResultEntryDefined eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await _resultBuilder.UpdateResultEntryAndResetConventionalResults(voteResultId, eventData.ResultEntry, eventData.ResultEntryParams);
    }

    public async Task Process(VoteResultCountOfVotersEntered eventData)
    {
        await _resultBuilder.UpdateCountOfVoters(eventData.VoteResultId, eventData.ResultsCountOfVoters);
    }

    public async Task Process(VoteResultEntered eventData)
    {
        await _resultBuilder.UpdateResults(eventData.VoteResultId, eventData.Results);
    }

    public async Task Process(VoteResultCorrectionEntered eventData)
    {
        await _resultBuilder.UpdateResults(eventData.VoteResultId, eventData.Results);
    }

    public async Task Process(VoteResultSubmissionFinished eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
    }

    public async Task Process(VoteResultCorrectionFinished eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        var createdComment = await CreateCommentIfNeeded(voteResultId, eventData.Comment, false, eventData.EventInfo);
        await UpdateState(voteResultId, CountingCircleResultState.CorrectionDone, eventData.EventInfo, createdComment);
    }

    public async Task Process(VoteResultFlaggedForCorrection eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        var createdComment = await CreateCommentIfNeeded(voteResultId, eventData.Comment, true, eventData.EventInfo);
        await UpdateState(voteResultId, CountingCircleResultState.ReadyForCorrection, eventData.EventInfo, createdComment);
    }

    public async Task Process(VoteResultAuditedTentatively eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
        await _endResultBuilder.AdjustVoteEndResult(voteResultId, false);
    }

    public async Task Process(VoteResultPlausibilised eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.Plausibilised, eventData.EventInfo);
    }

    public async Task Process(VoteResultResettedToSubmissionFinished eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        await _endResultBuilder.AdjustVoteEndResult(voteResultId, true);
    }

    public async Task Process(VoteResultResettedToAuditedTentatively eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
    }

    public async Task Process(VoteResultResetted eventData)
    {
        var voteResultId = GuidParser.Parse(eventData.VoteResultId);
        await UpdateState(voteResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
        await _resultBuilder.ResetConventionalResultInTestingPhase(voteResultId);
    }
}
