// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Lib.Iam.Store;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ProportionalElectionResultWriter : PoliticalBusinessResultWriter<DataModels.ProportionalElectionResult>
{
    private readonly ILogger<ProportionalElectionResultWriter> _logger;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionResult> _resultRepo;
    private readonly ValidationResultsEnsurer _validationResultsEnsurer;
    private readonly SecondFactorTransactionWriter _secondFactorTransactionWriter;

    public ProportionalElectionResultWriter(
        ILogger<ProportionalElectionResultWriter> logger,
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, DataModels.ProportionalElectionResult> resultRepo,
        PermissionService permissionService,
        ContestService contestService,
        ValidationResultsEnsurer validationResultsEnsurer,
        SecondFactorTransactionWriter secondFactorTransactionWriter,
        IAuth auth)
        : base(permissionService, contestService, auth, aggregateRepository)
    {
        _logger = logger;
        _resultRepo = resultRepo;
        _validationResultsEnsurer = validationResultsEnsurer;
        _secondFactorTransactionWriter = secondFactorTransactionWriter;
    }

    public async Task DefineEntry(Guid resultId, ProportionalElectionResultEntryParams resultEntryParams)
    {
        var result = await LoadPoliticalBusinessResult(resultId);
        var contestId = await EnsurePoliticalBusinessPermissions(result);
        EnsureValidResultEntry(result.ProportionalElection, resultEntryParams);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        aggregate.DefineEntry(resultEntryParams, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task EnterCountOfVoters(Guid resultId, PoliticalBusinessCountOfVoters countOfVoters)
    {
        var electionResult = await _resultRepo
                                .Query()
                                .Include(r => r.ProportionalElection.Contest)
                                .Include(r => r.CountingCircle)
                                .FirstOrDefaultAsync(r => r.Id == resultId)
                         ?? throw new EntityNotFoundException(resultId);
        var contestId = await EnsurePoliticalBusinessPermissions(electionResult);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        aggregate.EnterCountOfVoters(countOfVoters, contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Entered count of voters for proportional election result {ProportionalElectionResultId}", resultId);
    }

    public async Task EnterUnmodifiedListResults(
        Guid resultId,
        IReadOnlyCollection<ProportionalElectionUnmodifiedListResult> results)
    {
        var electionResult = await _resultRepo.Query()
                                 .Include(x => x.ProportionalElection.Contest)
                                 .Include(x => x.UnmodifiedListResults)
                                 .FirstOrDefaultAsync(x => x.Id == resultId)
                             ?? throw new EntityNotFoundException(resultId);
        var contestId = await EnsurePoliticalBusinessPermissions(electionResult);
        EnsureListsExists(electionResult, results);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        aggregate.EnterUnmodifiedListResults(results, contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Entered unmodified list results for proportional election result {ProportionalElectionResultId}", resultId);
    }

    public async Task<(SecondFactorTransaction? SecondFactorTransaction, string? Code)> PrepareSubmissionFinished(Guid resultId, string message)
    {
        await EnsurePoliticalBusinessPermissions(resultId);

        var result = await LoadPoliticalBusinessResult(resultId);
        if (IsSelfOwnedPoliticalBusiness(result.ProportionalElection))
        {
            return default;
        }

        var actionId = await PrepareActionId<ProportionalElectionResultAggregate>(
            nameof(SubmissionFinished),
            resultId,
            result.ProportionalElection.ContestId,
            result.CountingCircle.BasisCountingCircleId,
            result.ProportionalElection.Contest.TestingPhaseEnded);
        return await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
    }

    public async Task SubmissionFinished(Guid resultId, string secondFactorTransactionExternalId, CancellationToken ct)
    {
        var result = await LoadPoliticalBusinessResult(resultId);

        var contestId = await EnsurePoliticalBusinessPermissions(result);
        if (!IsSelfOwnedPoliticalBusiness(result.ProportionalElection))
        {
            await _secondFactorTransactionWriter.EnsureVerified(
                secondFactorTransactionExternalId,
                () => PrepareActionId<ProportionalElectionResultAggregate>(
                    nameof(SubmissionFinished),
                    resultId,
                    result.ProportionalElection.ContestId,
                    result.CountingCircle.BasisCountingCircleId,
                    result.ProportionalElection.Contest.TestingPhaseEnded),
                ct);
        }

        await _validationResultsEnsurer.EnsureProportionalElectionResultIsValid(result);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(result.Id);
        aggregate.SubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Submission finished for proportional election result {ProportionalElectionResultId}", result.Id);
    }

    public async Task<(SecondFactorTransaction? SecondFactorTransaction, string? Code)> PrepareCorrectionFinished(Guid resultId, string message)
    {
        await EnsurePoliticalBusinessPermissions(resultId);

        var result = await LoadPoliticalBusinessResult(resultId);
        if (IsSelfOwnedPoliticalBusiness(result.ProportionalElection))
        {
            return default;
        }

        var actionId = await PrepareActionId<ProportionalElectionResultAggregate>(
            nameof(CorrectionFinished),
            resultId,
            result.ProportionalElection.ContestId,
            result.CountingCircle.BasisCountingCircleId,
            result.ProportionalElection.Contest.TestingPhaseEnded);
        return await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
    }

    public async Task CorrectionFinished(Guid resultId, string comment, string secondFactorTransactionExternalId, CancellationToken ct)
    {
        var result = await LoadPoliticalBusinessResult(resultId);

        var contestId = await EnsurePoliticalBusinessPermissions(result);
        if (!IsSelfOwnedPoliticalBusiness(result.ProportionalElection))
        {
            await _secondFactorTransactionWriter.EnsureVerified(
                secondFactorTransactionExternalId,
                () => PrepareActionId<ProportionalElectionResultAggregate>(
                    nameof(CorrectionFinished),
                    resultId,
                    result.ProportionalElection.ContestId,
                    result.CountingCircle.BasisCountingCircleId,
                    result.ProportionalElection.Contest.TestingPhaseEnded),
                ct);
        }

        await _validationResultsEnsurer.EnsureProportionalElectionResultIsValid(result);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(result.Id);
        aggregate.CorrectionFinished(comment, contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Correction finished for proportional election result {ProportionalElectionResultId}", result.Id);
    }

    public async Task ResetToSubmissionFinished(Guid resultId)
    {
        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(resultId);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        if (aggregate.Published)
        {
            aggregate.Unpublish(contestId);
            _logger.LogInformation("majority election result {ProportionalElectionResultId} unpublished", aggregate.Id);
        }

        aggregate.ResetToSubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Proportional election result {ProportionalElectionResultId} reset to submission finished", aggregate.Id);
    }

    public async Task FlagForCorrection(Guid resultId, string comment)
    {
        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(resultId);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        aggregate.FlagForCorrection(contestId, comment);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Proportional election result {ProportionalElectionResultId} flagged for correction", aggregate.Id);
    }

    public async Task AuditedTentatively(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<ProportionalElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            aggregate.AuditedTentatively(contestId);
            _logger.LogInformation("Proportional election result {ProportionalElectionResultId} audited tentatively", aggregate.Id);

            var result = await LoadPoliticalBusinessResult(aggregate.Id);
            if (CanAutomaticallyPublishResults(result.ProportionalElection.DomainOfInfluence, result.ProportionalElection.Contest))
            {
                aggregate.Publish(contestId);
                _logger.LogInformation("Proportional election result {ProportionalElectionResultId} published", aggregate.Id);
            }
        });
    }

    public async Task Plausibilise(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<ProportionalElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            await EnsureStatePlausibilisedEnabled(contestId);
            aggregate.Plausibilise(contestId);
            _logger.LogInformation("Proportional election result {ProportionalElectionResultId} plausibilised", aggregate.Id);
        });
    }

    public async Task ResetToAuditedTentatively(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<ProportionalElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            aggregate.ResetToAuditedTentatively(contestId);
            _logger.LogInformation("Proportional election result {ProportionalElectionResultId} reset to audited tentatively", aggregate.Id);
        });
    }

    public async Task SubmissionFinishedAndAuditedTentatively(Guid resultId)
    {
        var result = await LoadPoliticalBusinessResult(resultId);
        if (!IsSelfOwnedPoliticalBusiness(result.ProportionalElection))
        {
            throw new ValidationException("finish submission and audit tentatively is not allowed for a non self owned political business");
        }

        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(resultId);
        await _validationResultsEnsurer.EnsureProportionalElectionResultIsValid(result);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(result.Id);
        aggregate.SubmissionFinished(contestId);
        aggregate.AuditedTentatively(contestId);
        _logger.LogInformation("Submission finished and audited tentatively for proportional election result {ProportionalElectionResultId}", result.Id);

        if (CanAutomaticallyPublishResults(result.ProportionalElection.DomainOfInfluence, result.ProportionalElection.Contest))
        {
            aggregate.Publish(contestId);
            _logger.LogInformation("Proportional election result {ProportionalElectionResultId} published", aggregate.Id);
        }

        await AggregateRepository.Save(aggregate);
    }

    public async Task Publish(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<ProportionalElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            var result = await LoadPoliticalBusinessResult(aggregate.Id);
            EnsureCanManuallyPublishResults(result.ProportionalElection.Contest, result.ProportionalElection.DomainOfInfluence);
            aggregate.Publish(contestId);
            _logger.LogInformation("Proportional election result {ProportionalElectionResultId} published", aggregate.Id);
        });
    }

    public async Task Unpublish(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<ProportionalElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            var result = await LoadPoliticalBusinessResult(aggregate.Id);
            EnsureCanManuallyPublishResults(result.ProportionalElection.Contest, result.ProportionalElection.DomainOfInfluence);
            aggregate.Unpublish(contestId);
            _logger.LogInformation("Proportional election result {ProportionalElectionResultId} unpublished", aggregate.Id);
        });
    }

    protected override async Task<DataModels.ProportionalElectionResult> LoadPoliticalBusinessResult(Guid resultId)
    {
        return await _resultRepo.Query()
                   .AsSplitQuery()
                   .Include(vr => vr.ProportionalElection.Contest.CantonDefaults)
                   .Include(vr => vr.ProportionalElection.DomainOfInfluence)
                   .Include(vr => vr.CountingCircle.ResponsibleAuthority)
                   .FirstOrDefaultAsync(x => x.Id == resultId)
               ?? throw new EntityNotFoundException(resultId);
    }

    private void EnsureValidResultEntry(DataModels.ProportionalElection election, ProportionalElectionResultEntryParams resultEntryParams)
    {
        if (election.EnforceEmptyVoteCountingForCountingCircles
            && election.AutomaticEmptyVoteCounting != resultEntryParams.AutomaticEmptyVoteCounting)
        {
            throw new ValidationException($"enforced {nameof(election.AutomaticEmptyVoteCounting)} setting not respected");
        }

        if (election.EnforceReviewProcedureForCountingCircles
            && election.ReviewProcedure != resultEntryParams.ReviewProcedure)
        {
            throw new ValidationException($"enforced {nameof(election.ReviewProcedure)} setting not respected");
        }

        if (election.EnforceCandidateCheckDigitForCountingCircles
            && election.CandidateCheckDigit != resultEntryParams.CandidateCheckDigit)
        {
            throw new ValidationException($"enforced {nameof(election.CandidateCheckDigit)} setting not respected");
        }
    }

    private void EnsureListsExists(
        DataModels.ProportionalElectionResult result,
        IEnumerable<ProportionalElectionUnmodifiedListResult> providedResults)
    {
        var hasUnmatchedList = providedResults.Select(x => x.ListId)
            .Except(result.UnmodifiedListResults.Select(x => x.ListId))
            .Any();
        if (hasUnmatchedList)
        {
            throw new ValidationException("lists provided which don't exist");
        }
    }
}
