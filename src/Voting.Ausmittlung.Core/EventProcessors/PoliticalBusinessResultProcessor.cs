// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public abstract class PoliticalBusinessResultProcessor<T>
    where T : CountingCircleResult, new()
{
    private readonly IDbRepository<DataContext, T> _repo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly IDbRepository<DataContext, CountingCircleResultComment> _commentRepo;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepo;
    private readonly AggregatedContestCountingCircleDetailsBuilder _aggregatedCcDetailsBuilder;

    protected PoliticalBusinessResultProcessor(
        IDbRepository<DataContext, T> repo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        IDbRepository<DataContext, CountingCircleResultComment> commentRepo,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        AggregatedContestCountingCircleDetailsBuilder aggregatedCcDetailsBuilder)
    {
        _repo = repo;
        _simpleResultRepo = simpleResultRepo;
        _commentRepo = commentRepo;
        _protocolExportRepo = protocolExportRepo;
        _aggregatedCcDetailsBuilder = aggregatedCcDetailsBuilder;
    }

    /// <summary>
    /// Updates the state of the result.
    /// </summary>
    /// <param name="resultId">The id of the result.</param>
    /// <param name="newState">The new state of the result.</param>
    /// <param name="eventInfo">The info of the event which triggered the state update.</param>
    /// <param name="commentCreated">Set to true if a comment has been created.</param>
    /// <returns>The old state of the result.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the result does not exist.</exception>
    protected async Task<CountingCircleResultState> UpdateState(Guid resultId, CountingCircleResultState newState, EventInfo eventInfo, bool commentCreated = false)
    {
        var result = await _repo.GetByKey(resultId)
            ?? throw new EntityNotFoundException(resultId);
        var simpleResult = await _simpleResultRepo.GetByKey(resultId)
            ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);
        var oldState = result.State;

        simpleResult.HasComments |= commentCreated;

        // the state / timestamps are expected redundancy to reduce joins on the query side
        simpleResult.State = result.State = newState;
        switch (newState)
        {
            case CountingCircleResultState.SubmissionOngoing:
                simpleResult.SubmissionDoneTimestamp = result.SubmissionDoneTimestamp = null;
                break;
            case CountingCircleResultState.ReadyForCorrection:
                simpleResult.SubmissionDoneTimestamp = result.SubmissionDoneTimestamp = null;
                simpleResult.ReadyForCorrectionTimestamp = result.ReadyForCorrectionTimestamp = eventInfo.Timestamp.ToDateTime();
                break;
            case CountingCircleResultState.SubmissionDone when !result.SubmissionDoneTimestamp.HasValue:
                simpleResult.SubmissionDoneTimestamp = result.SubmissionDoneTimestamp = eventInfo.Timestamp.ToDateTime();
                break;
            case CountingCircleResultState.CorrectionDone when !result.SubmissionDoneTimestamp.HasValue:
                simpleResult.SubmissionDoneTimestamp = result.SubmissionDoneTimestamp = eventInfo.Timestamp.ToDateTime();
                simpleResult.ReadyForCorrectionTimestamp = result.ReadyForCorrectionTimestamp = null;
                break;
            case CountingCircleResultState.AuditedTentatively when !result.AuditedTentativelyTimestamp.HasValue:
                simpleResult.AuditedTentativelyTimestamp = result.AuditedTentativelyTimestamp = eventInfo.Timestamp.ToDateTime();
                break;
            case CountingCircleResultState.Plausibilised when !result.PlausibilisedTimestamp.HasValue:
                simpleResult.PlausibilisedTimestamp = result.PlausibilisedTimestamp = eventInfo.Timestamp.ToDateTime();
                break;
        }

        if (newState < CountingCircleResultState.AuditedTentatively)
        {
            simpleResult.AuditedTentativelyTimestamp = result.AuditedTentativelyTimestamp = null;
        }

        if (newState < CountingCircleResultState.Plausibilised)
        {
            simpleResult.PlausibilisedTimestamp = result.PlausibilisedTimestamp = null;
        }

        await _repo.Update(result);
        await _simpleResultRepo.Update(simpleResult);

        switch (newState)
        {
            case CountingCircleResultState.AuditedTentatively:
                await _protocolExportRepo.Query()
                .Where(x => x.PoliticalBusinessIds.Contains(result.PoliticalBusinessId))
                .ExecuteDeleteAsync();
                break;
            case CountingCircleResultState.SubmissionDone when !oldState.IsSubmissionDone():
            case CountingCircleResultState.CorrectionDone:
                await _aggregatedCcDetailsBuilder.AdjustAggregatedDetails(resultId, false);
                break;
            case CountingCircleResultState.ReadyForCorrection:
            case CountingCircleResultState.SubmissionOngoing when oldState.IsSubmissionDone():
                await _aggregatedCcDetailsBuilder.AdjustAggregatedDetails(resultId, true);
                break;
        }

        return oldState;
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

    protected async Task UpdateSimpleResult(Guid resultId, PoliticalBusinessNullableCountOfVoters countOfVoters)
    {
        var simpleResult = await _simpleResultRepo.GetByKey(resultId)
                           ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        simpleResult.CountOfVoters = countOfVoters;
        await _simpleResultRepo.Update(simpleResult);
    }

    protected async Task UpdatePublished(Guid resultId, bool published)
    {
        var result = await _repo.GetByKey(resultId)
                     ?? throw new EntityNotFoundException(resultId);
        var simpleResult = await _simpleResultRepo.GetByKey(resultId)
                           ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        simpleResult.Published = result.Published = published;
        await _repo.Update(result);
        await _simpleResultRepo.Update(simpleResult);
    }
}
