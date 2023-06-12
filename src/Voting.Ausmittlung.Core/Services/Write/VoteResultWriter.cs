// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class VoteResultWriter : PoliticalBusinessResultWriter<DataModels.VoteResult>
{
    private readonly ILogger<VoteResultWriter> _logger;
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, DataModels.VoteResult> _voteResultRepo;
    private readonly ValidationResultsEnsurer _validationResultsEnsurer;
    private readonly SecondFactorTransactionWriter _secondFactorTransactionWriter;

    public VoteResultWriter(
        ILogger<VoteResultWriter> logger,
        IAggregateRepository aggregateRepository,
        PermissionService permissionService,
        ContestService contestService,
        IDbRepository<DataContext, DataModels.VoteResult> voteResultRepo,
        ValidationResultsEnsurer validationResultsEnsurer,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
        : base(permissionService, contestService, aggregateRepository)
    {
        _logger = logger;
        _permissionService = permissionService;
        _voteResultRepo = voteResultRepo;
        _validationResultsEnsurer = validationResultsEnsurer;
        _secondFactorTransactionWriter = secondFactorTransactionWriter;
    }

    public async Task DefineEntry(Guid voteResultId, DataModels.VoteResultEntry resultEntry, VoteResultEntryParams? resultEntryParams)
    {
        _permissionService.EnsureErfassungElectionAdmin();
        var voteResult = await LoadPoliticalBusinessResult(voteResultId);
        EnsureResultEntryRespectSettings(voteResult.Vote, resultEntry, resultEntryParams);
        var contestId = await EnsurePoliticalBusinessPermissions(voteResult, true);

        var aggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResult.Id);
        aggregate.DefineEntry(resultEntry, contestId, resultEntryParams);
        await AggregateRepository.Save(aggregate);
    }

    public async Task EnterCountOfVoters(Guid voteResultId, IReadOnlyCollection<VoteBallotResultsCountOfVoters> ballotCountOfVoters)
    {
        var voteResult = await _voteResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.Contest)
            .Include(x => x.CountingCircle)
            .Include(x => x.Results).ThenInclude(r => r.Ballot)
            .Include(x => x.Results).ThenInclude(r => r.QuestionResults).ThenInclude(qr => qr.Question)
            .Include(x => x.Results).ThenInclude(r => r.TieBreakQuestionResults).ThenInclude(qr => qr.Question)
            .FirstOrDefaultAsync(x => x.Id == voteResultId)
            ?? throw new EntityNotFoundException(voteResultId);
        var contestId = await EnsurePoliticalBusinessPermissions(voteResult, true);
        EnsureResultsExists(voteResult, ballotCountOfVoters);

        var aggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResult.Id);
        aggregate.EnterCountOfVoters(ballotCountOfVoters, contestId);
        await AggregateRepository.Save(aggregate);

        _logger.LogInformation("Entered count of voters for vote result {VoteResultId}", voteResult.Id);
    }

    public async Task EnterResults(Guid voteResultId, IReadOnlyCollection<VoteBallotResults> requestResults)
    {
        var voteResult = await _voteResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.Contest)
            .Include(x => x.CountingCircle)
            .Include(x => x.Results).ThenInclude(r => r.Ballot)
            .Include(x => x.Results).ThenInclude(r => r.QuestionResults).ThenInclude(qr => qr.Question)
            .Include(x => x.Results).ThenInclude(r => r.TieBreakQuestionResults).ThenInclude(qr => qr.Question)
            .FirstOrDefaultAsync(x => x.Id == voteResultId)
            ?? throw new EntityNotFoundException(voteResultId);
        var contestId = await EnsurePoliticalBusinessPermissions(voteResult, true);
        EnsureResultsExists(voteResult, requestResults);

        var aggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResult.Id);

        // enter count of voters is always done in the same step if final results are entered
        aggregate.EnterCountOfVoters(requestResults, contestId);
        aggregate.EnterResults(requestResults, contestId);
        await AggregateRepository.Save(aggregate);

        _logger.LogInformation("Entered results for vote result {VoteResultId}", voteResult.Id);
    }

    public async Task EnterCorrectionResults(Guid voteResultId, IReadOnlyCollection<VoteBallotResults> requestResults)
    {
        var voteResult = await _voteResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.Contest)
            .Include(x => x.Results).ThenInclude(r => r.Ballot)
            .Include(x => x.Results).ThenInclude(r => r.QuestionResults).ThenInclude(qr => qr.Question)
            .Include(x => x.Results).ThenInclude(r => r.TieBreakQuestionResults).ThenInclude(qr => qr.Question)
            .FirstOrDefaultAsync(x => x.Id == voteResultId)
            ?? throw new EntityNotFoundException(voteResultId);
        var contestId = await EnsurePoliticalBusinessPermissions(voteResult, true);
        EnsureResultsExists(voteResult, requestResults);

        var aggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResult.Id);

        // enter count of voters is always done in the same step if final results are entered
        aggregate.EnterCountOfVoters(requestResults, contestId);
        aggregate.EnterCorrectionResults(requestResults, contestId);
        await AggregateRepository.Save(aggregate);

        _logger.LogInformation("Entered correction results for vote result {VoteResultId}", voteResult.Id);
    }

    public async Task<(SecondFactorTransaction SecondFactorTransaction, string Code)> PrepareSubmissionFinished(Guid voteResultId, string message)
    {
        await EnsurePoliticalBusinessPermissions(voteResultId, true);

        var voteResult = await LoadPoliticalBusinessResult(voteResultId);
        var actionId = await PrepareActionId<VoteResultAggregate>(
            nameof(SubmissionFinished),
            voteResultId,
            voteResult.Vote.ContestId,
            voteResult.CountingCircle.BasisCountingCircleId,
            voteResult.Vote.Contest.TestingPhaseEnded);
        return await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
    }

    public async Task SubmissionFinished(Guid voteResultId, string secondFactorTransactionExternalId, CancellationToken ct)
    {
        var voteResult = await LoadPoliticalBusinessResult(voteResultId);

        var contestId = await EnsurePoliticalBusinessPermissions(voteResult, true);
        await _secondFactorTransactionWriter.EnsureVerified(
            secondFactorTransactionExternalId,
            () => PrepareActionId<VoteResultAggregate>(
                nameof(SubmissionFinished),
                voteResultId,
                voteResult.Vote.ContestId,
                voteResult.CountingCircle.BasisCountingCircleId,
                voteResult.Vote.Contest.TestingPhaseEnded),
            ct);
        await _validationResultsEnsurer.EnsureVoteResultIsValid(voteResult);

        var aggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResult.Id);
        aggregate.SubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Submission finished for vote result {VoteResultId}", voteResult.Id);
    }

    public async Task<(SecondFactorTransaction SecondFactorTransaction, string Code)> PrepareCorrectionFinished(Guid voteResultId, string message)
    {
        await EnsurePoliticalBusinessPermissions(voteResultId, true);

        var voteResult = await LoadPoliticalBusinessResult(voteResultId);
        var actionId = await PrepareActionId<VoteResultAggregate>(
            nameof(CorrectionFinished),
            voteResultId,
            voteResult.Vote.ContestId,
            voteResult.CountingCircle.BasisCountingCircleId,
            voteResult.Vote.Contest.TestingPhaseEnded);
        return await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
    }

    public async Task CorrectionFinished(Guid voteResultId, string comment, string secondFactorTransactionExternalId, CancellationToken ct)
    {
        var voteResult = await LoadPoliticalBusinessResult(voteResultId);

        var contestId = await EnsurePoliticalBusinessPermissions(voteResult, true);
        await _secondFactorTransactionWriter.EnsureVerified(
            secondFactorTransactionExternalId,
            () => PrepareActionId<VoteResultAggregate>(
            nameof(CorrectionFinished),
            voteResultId,
            voteResult.Vote.ContestId,
            voteResult.CountingCircle.BasisCountingCircleId,
            voteResult.Vote.Contest.TestingPhaseEnded),
            ct);
        await _validationResultsEnsurer.EnsureVoteResultIsValid(voteResult);

        var aggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResult.Id);
        aggregate.CorrectionFinished(comment, contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Correction finished for vote result {VoteResultId}", voteResult.Id);
    }

    public async Task ResetToSubmissionFinished(Guid voteResultId)
    {
        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(voteResultId);

        var aggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResultId);
        aggregate.ResetToSubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Vote result {VoteResultId} reset to submission finished", aggregate.Id);
    }

    public async Task FlagForCorrection(Guid voteResultId, string comment)
    {
        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(voteResultId);

        var aggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResultId);
        aggregate.FlagForCorrection(contestId, comment);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Vote result {VoteResultId} flagged for correction", aggregate.Id);
    }

    public async Task AuditedTentatively(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<VoteResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            aggregate.AuditedTentatively(contestId);
            _logger.LogInformation("Vote result {VoteResultId} audited tentatively", aggregate.Id);
        });
    }

    public async Task Plausibilise(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<VoteResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            aggregate.Plausibilise(contestId);
            _logger.LogInformation("Vote result {VoteResultId} plausibilised", aggregate.Id);
        });
    }

    public async Task ResetToAuditedTentatively(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<VoteResultAggregate>(resultIds, async aggregate =>
       {
           var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
           aggregate.ResetToAuditedTentatively(contestId);
           _logger.LogInformation("Vote result {VoteResultId} reset to audited tentatively", aggregate.Id);
       });
    }

    protected override async Task<DataModels.VoteResult> LoadPoliticalBusinessResult(Guid resultId)
    {
        return await _voteResultRepo.Query()
            .AsSplitQuery()
            .Include(vr => vr.Vote.Contest)
            .Include(vr => vr.Vote.DomainOfInfluence)
            .Include(vr => vr.CountingCircle.ResponsibleAuthority)
            .Include(vr => vr.Results).ThenInclude(x => x.Ballot)
            .Include(vr => vr.Results).ThenInclude(x => x.QuestionResults).ThenInclude(x => x.Question)
            .Include(vr => vr.Results).ThenInclude(x => x.TieBreakQuestionResults).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(resultId);
    }

    private void EnsureResultsExists(DataModels.VoteResult voteResult, IEnumerable<VoteBallotResults> results)
    {
        var ballots = voteResult.Results
            .Select(r => r.Ballot)
            .ToDictionary(x => x.Id);
        var questionNumbers = voteResult.Results
            .SelectMany(r => r.QuestionResults)
            .Select(x => x.Question.Number)
            .ToList();
        var tieBreakQuestionNumbers = voteResult.Results
            .SelectMany(r => r.TieBreakQuestionResults)
            .Select(x => x.Question.Number)
            .ToList();
        foreach (var result in results)
        {
            if (!ballots.TryGetValue(result.BallotId, out var ballot))
            {
                throw new ValidationException("unknown results provided");
            }

            var hasUnknownResults = result.QuestionResults
                .Select(x => x.QuestionNumber)
                .Except(questionNumbers)
                .Any();
            if (hasUnknownResults)
            {
                throw new ValidationException("unknown results provided");
            }

            var hasUnknownTieBreakResults = result.TieBreakQuestionResults
                .Select(x => x.QuestionNumber)
                .Except(tieBreakQuestionNumbers)
                .Any();
            if (hasUnknownTieBreakResults)
            {
                throw new ValidationException("unknown tie break results provided");
            }

            var hasUnspecifiedAnswers = result.QuestionResults.Any(r => r.ReceivedCountUnspecified.GetValueOrDefault() != 0)
                                     || result.TieBreakQuestionResults.Any(r => r.ReceivedCountUnspecified.GetValueOrDefault() != 0);
            if (ballot.BallotType == DataModels.BallotType.StandardBallot && hasUnspecifiedAnswers)
            {
                throw new ValidationException("unspecified answers are not allowed for standard ballots");
            }
        }
    }

    private void EnsureResultsExists(DataModels.VoteResult voteResult, IEnumerable<VoteBallotResultsCountOfVoters> results)
    {
        var ballotIds = voteResult.Results
            .Select(x => x.Ballot.Id)
            .ToHashSet();

        if (results.Any(x => !ballotIds.Remove(x.BallotId)))
        {
            throw new ValidationException("unknown results provided");
        }
    }

    private void EnsureResultEntryRespectSettings(
        DataModels.Vote vote,
        DataModels.VoteResultEntry resultEntry,
        VoteResultEntryParams? resultEntryParams)
    {
        if (vote.EnforceResultEntryForCountingCircles
            && vote.ResultEntry != resultEntry)
        {
            throw new ValidationException("enforced result entry setting not respected");
        }

        if (resultEntryParams != null
            && vote.EnforceReviewProcedureForCountingCircles
            && vote.ReviewProcedure != resultEntryParams.ReviewProcedure)
        {
            throw new ValidationException($"enforced {nameof(vote.ReviewProcedure)} setting not respected");
        }
    }
}
