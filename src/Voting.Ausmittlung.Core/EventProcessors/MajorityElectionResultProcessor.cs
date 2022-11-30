// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

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
    IEventProcessor<MajorityElectionResultResetted>
{
    private readonly IMapper _mapper;
    private readonly MajorityElectionResultRepo _electionResultRepo;
    private readonly MajorityElectionResultBuilder _resultBuilder;
    private readonly MajorityElectionBallotGroupResultBuilder _ballotGroupResultBuilder;
    private readonly MajorityElectionEndResultBuilder _endResultBuilder;

    public MajorityElectionResultProcessor(
        IMapper mapper,
        MajorityElectionResultRepo electionResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        MajorityElectionResultBuilder resultBuilder,
        MajorityElectionBallotGroupResultBuilder ballotGroupResultBuilder,
        IDbRepository<DataContext, CountingCircleResultComment> commentRepo,
        MessageProducerBuffer resultStateChangeMessageBuffer,
        MajorityElectionEndResultBuilder endResultBuilder)
        : base(electionResultRepo, simpleResultRepo, commentRepo, resultStateChangeMessageBuffer)
    {
        _mapper = mapper;
        _electionResultRepo = electionResultRepo;
        _resultBuilder = resultBuilder;
        _ballotGroupResultBuilder = ballotGroupResultBuilder;
        _endResultBuilder = endResultBuilder;
    }

    public async Task Process(MajorityElectionResultSubmissionStarted eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
    }

    public async Task Process(MajorityElectionResultEntryDefined eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await _resultBuilder.UpdateResultEntryAndResetConventionalResult(electionResultId, eventData.ResultEntry, eventData.ResultEntryParams);
    }

    public async Task Process(MajorityElectionResultCountOfVotersEntered eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var electionResult = await _electionResultRepo.GetByKey(electionResultId)
                             ?? throw new EntityNotFoundException(electionResultId);

        _mapper.Map(eventData.CountOfVoters, electionResult.CountOfVoters);
        electionResult.UpdateVoterParticipation();
        await _electionResultRepo.Update(electionResult);
    }

    public async Task Process(MajorityElectionCandidateResultsEntered eventData)
    {
        await _resultBuilder.UpdateConventionalResults(eventData);
    }

    public async Task Process(MajorityElectionBallotGroupResultsEntered eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var counts = eventData.Results.ToDictionary(x => GuidParser.Parse(x.BallotGroupId), x => x.VoteCount);
        await _ballotGroupResultBuilder.UpdateBallotGroupAndCandidateResults(electionResultId, counts);
        await _resultBuilder.UpdateTotalCountOfBallotGroupVotes(electionResultId, counts.Values.Sum());
    }

    public async Task Process(MajorityElectionResultSubmissionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
    }

    public async Task Process(MajorityElectionResultCorrectionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var createdComment = await CreateCommentIfNeeded(electionResultId, eventData.Comment, false, eventData.EventInfo);
        await UpdateState(electionResultId, CountingCircleResultState.CorrectionDone, eventData.EventInfo, createdComment);
    }

    public async Task Process(MajorityElectionResultFlaggedForCorrection eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var createdComment = await CreateCommentIfNeeded(electionResultId, eventData.Comment, true, eventData.EventInfo);
        await UpdateState(electionResultId, CountingCircleResultState.ReadyForCorrection, eventData.EventInfo, createdComment);
    }

    public async Task Process(MajorityElectionResultAuditedTentatively eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
        await _endResultBuilder.AdjustEndResult(electionResultId, false);
    }

    public async Task Process(MajorityElectionResultPlausibilised eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.Plausibilised, eventData.EventInfo);
    }

    public async Task Process(MajorityElectionResultResettedToSubmissionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        await _endResultBuilder.AdjustEndResult(electionResultId, true);
    }

    public async Task Process(MajorityElectionResultResettedToAuditedTentatively eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
    }

    public async Task Process(MajorityElectionResultResetted eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
        await _resultBuilder.ResetConventionalResultInTestingPhase(electionResultId);
    }
}
