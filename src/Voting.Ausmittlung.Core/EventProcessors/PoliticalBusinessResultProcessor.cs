// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.EventProcessors;

public abstract class PoliticalBusinessResultProcessor<T>
    where T : CountingCircleResult, new()
{
    private readonly IDbRepository<DataContext, T> _repo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly IDbRepository<DataContext, CountingCircleResultComment> _commentRepo;
    private readonly MessageProducerBuffer _resultStateChangeMessageProducerBuffer;

    protected PoliticalBusinessResultProcessor(
        IDbRepository<DataContext, T> repo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        IDbRepository<DataContext, CountingCircleResultComment> commentRepo,
        MessageProducerBuffer resultStateChangeMessageProducerBuffer)
    {
        _repo = repo;
        _resultStateChangeMessageProducerBuffer = resultStateChangeMessageProducerBuffer;
        _simpleResultRepo = simpleResultRepo;
        _commentRepo = commentRepo;
    }

    protected async Task UpdateState(Guid resultId, CountingCircleResultState newState, EventInfo eventInfo, bool commentCreated = false)
    {
        var result = await _repo.GetByKey(resultId)
            ?? throw new EntityNotFoundException(resultId);
        var simpleResult = await _simpleResultRepo.GetByKey(resultId)
            ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        simpleResult.HasComments |= commentCreated;

        // the state / timestamps are expected redundancy to reduce joins on the query side
        simpleResult.State = result.State = newState;
        switch (newState)
        {
            case CountingCircleResultState.SubmissionOngoing:
            case CountingCircleResultState.ReadyForCorrection:
                simpleResult.SubmissionDoneTimestamp = result.SubmissionDoneTimestamp = null;
                break;
            case CountingCircleResultState.SubmissionDone when !result.SubmissionDoneTimestamp.HasValue:
            case CountingCircleResultState.CorrectionDone when !result.SubmissionDoneTimestamp.HasValue:
                simpleResult.SubmissionDoneTimestamp = result.SubmissionDoneTimestamp = eventInfo.Timestamp.ToDateTime();
                break;
            case CountingCircleResultState.AuditedTentatively when !result.AuditedTentativelyTimestamp.HasValue:
                result.AuditedTentativelyTimestamp = eventInfo.Timestamp.ToDateTime();
                break;
            case CountingCircleResultState.Plausibilised when !result.PlausibilisedTimestamp.HasValue:
                result.PlausibilisedTimestamp = eventInfo.Timestamp.ToDateTime();
                break;
        }

        if (newState < CountingCircleResultState.AuditedTentatively)
        {
            result.AuditedTentativelyTimestamp = null;
        }

        if (newState < CountingCircleResultState.Plausibilised)
        {
            result.PlausibilisedTimestamp = null;
        }

        await _repo.Update(result);
        await _simpleResultRepo.Update(simpleResult);
        _resultStateChangeMessageProducerBuffer.Add(new ResultStateChanged(result.Id, result.CountingCircleId, result.PoliticalBusinessId, result.State));
    }

    protected async Task<bool> CreateCommentIfNeeded(
        Guid resultId,
        string commentContent,
        bool createdByMonitoringAuthority,
        EventInfo eventInfo)
    {
        if (string.IsNullOrWhiteSpace(commentContent))
        {
            return false;
        }

        var comment = new CountingCircleResultComment
        {
            Content = commentContent,
            CreatedAt = eventInfo.Timestamp.ToDateTime(),
            CreatedBy = eventInfo.User.ToDataUser(),
            ResultId = resultId,
            CreatedByMonitoringAuthority = createdByMonitoringAuthority,
        };
        await _commentRepo.Create(comment);
        return true;
    }
}
