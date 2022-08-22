// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

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
    IEventProcessor<ProportionalElectionResultResettedToAuditedTentatively>
{
    private readonly IMapper _mapper;
    private readonly ProportionalElectionResultRepo _electionResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnmodifiedListResult> _unmodifiedListResultRepo;
    private readonly ProportionalElectionResultBuilder _resultBuilder;
    private readonly ProportionalElectionEndResultBuilder _endResultBuilder;

    public ProportionalElectionResultProcessor(
        IMapper mapper,
        ProportionalElectionResultRepo electionResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        IDbRepository<DataContext, ProportionalElectionUnmodifiedListResult> unmodifiedListResultRepo,
        IDbRepository<DataContext, CountingCircleResultComment> commentRepo,
        ProportionalElectionResultBuilder resultBuilder,
        ProportionalElectionEndResultBuilder endResultBuilder,
        MessageProducerBuffer messageProducerBuffer)
        : base(electionResultRepo, simpleResultRepo, commentRepo, messageProducerBuffer)
    {
        _mapper = mapper;
        _electionResultRepo = electionResultRepo;
        _unmodifiedListResultRepo = unmodifiedListResultRepo;
        _resultBuilder = resultBuilder;
        _endResultBuilder = endResultBuilder;
    }

    public async Task Process(ProportionalElectionResultSubmissionStarted eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionOngoing, eventData.EventInfo);
    }

    public async Task Process(ProportionalElectionResultEntryDefined eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await _resultBuilder.UpdateResultEntryAndResetConventionalResults(electionResultId, eventData.ResultEntryParams);
    }

    public async Task Process(ProportionalElectionResultCountOfVotersEntered eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var electionResult = await _electionResultRepo.GetByKey(electionResultId)
                             ?? throw new EntityNotFoundException(electionResultId);

        _mapper.Map(eventData.CountOfVoters, electionResult.CountOfVoters);
        electionResult.UpdateVoterParticipation();
        await _electionResultRepo.Update(electionResult);
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
        foreach (var enteredUnmodifiedListResult in eventData.Results)
        {
            var listId = GuidParser.Parse(enteredUnmodifiedListResult.ListId);
            if (!unmodifiedListResultsByListId.TryGetValue(listId, out var unmodifiedListResult))
            {
                throw new EntityNotFoundException(listId);
            }

            if (enteredUnmodifiedListResult.VoteCount == unmodifiedListResult.VoteCount)
            {
                continue;
            }

            if (!listResultsByListId.TryGetValue(listId, out var listResult))
            {
                throw new EntityNotFoundException(listId);
            }

            await _resultBuilder.UpdateVotesFromUnmodifiedListResult(listResult, enteredUnmodifiedListResult.VoteCount);

            unmodifiedListResult.ConventionalVoteCount = enteredUnmodifiedListResult.VoteCount;
            unmodifiedListResultsToUpdate.Add(unmodifiedListResult);
        }

        await _unmodifiedListResultRepo.UpdateRange(unmodifiedListResultsToUpdate);

        electionResult.ConventionalSubTotal.TotalCountOfUnmodifiedLists = electionResult.UnmodifiedListResults.Sum(x => x.ConventionalVoteCount);
        await _electionResultRepo.Update(electionResult);
    }

    public async Task Process(ProportionalElectionResultSubmissionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
    }

    public async Task Process(ProportionalElectionResultCorrectionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var createdComment = await CreateCommentIfNeeded(electionResultId, eventData.Comment, false, eventData.EventInfo);
        await UpdateState(electionResultId, CountingCircleResultState.CorrectionDone, eventData.EventInfo, createdComment);
    }

    public async Task Process(ProportionalElectionResultFlaggedForCorrection eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        var createdComment = await CreateCommentIfNeeded(electionResultId, eventData.Comment, true, eventData.EventInfo);
        await UpdateState(electionResultId, CountingCircleResultState.ReadyForCorrection, eventData.EventInfo, createdComment);
    }

    public async Task Process(ProportionalElectionResultAuditedTentatively eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
        await _endResultBuilder.AdjustEndResult(electionResultId, false);
    }

    public async Task Process(ProportionalElectionResultPlausibilised eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.Plausibilised, eventData.EventInfo);
    }

    public async Task Process(ProportionalElectionResultResettedToSubmissionFinished eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.SubmissionDone, eventData.EventInfo);
        await _endResultBuilder.AdjustEndResult(electionResultId, true);
    }

    public async Task Process(ProportionalElectionResultResettedToAuditedTentatively eventData)
    {
        var electionResultId = GuidParser.Parse(eventData.ElectionResultId);
        await UpdateState(electionResultId, CountingCircleResultState.AuditedTentatively, eventData.EventInfo);
    }
}
