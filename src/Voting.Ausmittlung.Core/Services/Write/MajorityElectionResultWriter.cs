// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Data;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class MajorityElectionResultWriter : PoliticalBusinessResultWriter<DataModels.MajorityElectionResult>
{
    private readonly ILogger<MajorityElectionResultWriter> _logger;
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionResult> _resultRepo;
    private readonly ValidationResultsEnsurer _validationResultsEnsurer;

    public MajorityElectionResultWriter(
        ILogger<MajorityElectionResultWriter> logger,
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, DataModels.MajorityElectionResult> resultRepo,
        PermissionService permissionService,
        ContestService contestService,
        ValidationResultsEnsurer validationResultsEnsurer,
        SecondFactorTransactionWriter secondFactorTransactionWriter,
        IAuth auth)
        : base(permissionService, contestService, auth, aggregateRepository, secondFactorTransactionWriter)
    {
        _logger = logger;
        _resultRepo = resultRepo;
        _validationResultsEnsurer = validationResultsEnsurer;
    }

    public async Task DefineEntry(
        Guid resultId,
        DataModels.MajorityElectionResultEntry resultEntry,
        MajorityElectionResultEntryParams? resultEntryParams)
    {
        var result = await LoadPoliticalBusinessResult(resultId);
        var contestId = await EnsurePoliticalBusinessPermissions(result);
        EnsureResultEntryRespectSettings(result.MajorityElection, resultEntry, resultEntryParams);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(result.Id);
        aggregate.DefineEntry(resultEntry, contestId, resultEntryParams);
        await AggregateRepository.Save(aggregate);
    }

    public async Task EnterCountOfVoters(Guid resultId, PoliticalBusinessCountOfVoters countOfVoters)
    {
        var electionResult = await _resultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.MajorityElection.Contest)
            .Include(x => x.CountingCircle)
            .Include(x => x.CandidateResults)
            .Include(x => x.SecondaryMajorityElectionResults)
            .ThenInclude(x => x.CandidateResults)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(resultId);
        var contestId = await EnsurePoliticalBusinessPermissions(electionResult);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(electionResult.Id);
        aggregate.EnterCountOfVoters(countOfVoters, contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Entered count of voters for majority election result {MajorityElectionResultId}", electionResult.Id);
    }

    public async Task EnterCandidateResults(
        Guid resultId,
        int? individualVoteCount,
        int? emptyVoteCount,
        int? invalidVoteCount,
        PoliticalBusinessCountOfVoters countOfVoters,
        IReadOnlyCollection<MajorityElectionCandidateResult> candidateResults,
        IReadOnlyCollection<SecondaryMajorityElectionCandidateResults> secondaryCandidateResults)
    {
        var electionResult = await _resultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.MajorityElection.Contest)
            .Include(x => x.CountingCircle)
            .Include(x => x.CandidateResults)
            .Include(x => x.SecondaryMajorityElectionResults)
                .ThenInclude(x => x.CandidateResults)
            .Include(x => x.SecondaryMajorityElectionResults)
                .ThenInclude(x => x.SecondaryMajorityElection)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(resultId);
        var contestId = await EnsurePoliticalBusinessPermissions(electionResult);
        EnsureCandidatesExistsAndNoDuplicates(electionResult, candidateResults, secondaryCandidateResults);
        EnsureNoEmptyVoteCountForSingleMandate(electionResult, emptyVoteCount);
        EnsureValidIndividualVoteCount(electionResult, individualVoteCount, secondaryCandidateResults);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(electionResult.Id);

        // enter count of voters is always done in the same step if final results are entered
        aggregate.EnterCountOfVoters(countOfVoters, contestId);
        aggregate.EnterCandidateResults(individualVoteCount, emptyVoteCount, invalidVoteCount, candidateResults, secondaryCandidateResults, contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Entered candidate results for majority election result {MajorityElectionResultId}", electionResult.Id);
    }

    public async Task EnterBallotGroupResults(
        Guid resultId,
        IReadOnlyCollection<MajorityElectionBallotGroupResult> results)
    {
        var electionResult = await _resultRepo.Query()
            .Include(x => x.MajorityElection.Contest)
            .Include(x => x.BallotGroupResults)
            .ThenInclude(x => x.BallotGroup)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(resultId);
        var contestId = await EnsurePoliticalBusinessPermissions(electionResult);
        EnsureBallotGroupsExistsAndNoDuplicates(electionResult, results);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(electionResult.Id);
        aggregate.EnterBallotGroupResults(results, contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Entered ballot group results for majority election result {MajorityElectionResultId}", electionResult.Id);
    }

    public async Task<SecondFactorInfo?> PrepareSubmissionFinished(Guid resultId, string message)
    {
        return await PrepareSecondFactor<MajorityElectionResultAggregate>(nameof(SubmissionFinished), resultId, message);
    }

    public async Task SubmissionFinished(Guid resultId, Guid? secondFactorTransactionId, CancellationToken ct)
    {
        var result = await LoadPoliticalBusinessResult(resultId, true);
        var contestId = await EnsurePoliticalBusinessPermissions(result);

        await VerifySecondFactor<MajorityElectionResultAggregate>(result, nameof(SubmissionFinished), secondFactorTransactionId, ct);
        await _validationResultsEnsurer.EnsureMajorityElectionResultIsValid(result);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(result.Id);
        aggregate.SubmissionFinished(contestId);
        if (CanAutomaticallyPublishResults(result.MajorityElection.DomainOfInfluence, result.MajorityElection.Contest, aggregate))
        {
            aggregate.Publish(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} published", aggregate.Id);
        }

        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Submission finished for majority election result {MajorityElectionResultId}", result.Id);
    }

    public async Task<SecondFactorInfo?> PrepareCorrectionFinished(Guid resultId, string message)
    {
        return await PrepareSecondFactor<MajorityElectionResultAggregate>(nameof(CorrectionFinished), resultId, message);
    }

    public async Task CorrectionFinished(Guid resultId, string comment, Guid? secondFactorTransactionId, CancellationToken ct)
    {
        var result = await LoadPoliticalBusinessResult(resultId, true);
        var contestId = await EnsurePoliticalBusinessPermissions(result);

        await VerifySecondFactor<MajorityElectionResultAggregate>(result, nameof(CorrectionFinished), secondFactorTransactionId, ct);
        await _validationResultsEnsurer.EnsureMajorityElectionResultIsValid(result);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(result.Id);
        aggregate.CorrectionFinished(comment, contestId);
        if (CanAutomaticallyPublishResults(result.MajorityElection.DomainOfInfluence, result.MajorityElection.Contest, aggregate))
        {
            aggregate.Publish(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} published", aggregate.Id);
        }

        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Correction finished for majority election result {MajorityElectionResultId}", result.Id);
    }

    public async Task ResetToSubmissionFinished(Guid resultId)
    {
        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(resultId);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(resultId);
        var result = await LoadPoliticalBusinessResult(resultId);
        if (CanUnpublishResults(result.MajorityElection.Contest, aggregate))
        {
            aggregate.Unpublish(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} unpublished", aggregate.Id);
        }

        aggregate.ResetToSubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Majority election result {MajorityElectionResultId} reset to submission finished", aggregate.Id);
    }

    public async Task FlagForCorrection(Guid resultId, string comment)
    {
        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(resultId);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(resultId);
        var result = await LoadPoliticalBusinessResult(resultId);
        if (CanUnpublishResults(result.MajorityElection.Contest, aggregate))
        {
            aggregate.Unpublish(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} unpublished", aggregate.Id);
        }

        aggregate.FlagForCorrection(contestId, comment);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Majority election result {MajorityElectionResultId} flagged for correction", aggregate.Id);
    }

    public async Task AuditedTentatively(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<MajorityElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            aggregate.AuditedTentatively(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} audited tentatively", aggregate.Id);

            var result = await LoadPoliticalBusinessResult(aggregate.Id);
            if (CanAutomaticallyPublishResults(result.MajorityElection.DomainOfInfluence, result.MajorityElection.Contest, aggregate))
            {
                aggregate.Publish(contestId);
                _logger.LogInformation("majority election result {MajorityElectionResultId} published", aggregate.Id);
            }
        });
    }

    public async Task Plausibilise(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<MajorityElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            await EnsureStatePlausibilisedEnabled(contestId);
            aggregate.Plausibilise(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} plausibilised", aggregate.Id);
        });
    }

    public async Task ResetToAuditedTentatively(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<MajorityElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            aggregate.ResetToAuditedTentatively(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} reset to audited tentatively", aggregate.Id);
        });
    }

    public async Task<SecondFactorInfo> PrepareSubmissionFinishedAndAuditedTentatively(Guid resultId, string message)
    {
        return await PrepareSecondFactor<MajorityElectionResultAggregate>(nameof(SubmissionFinishedAndAuditedTentatively), resultId, message)
            ?? throw new InvalidOperationException("2FA is required in " + nameof(SubmissionFinishedAndAuditedTentatively));
    }

    public async Task SubmissionFinishedAndAuditedTentatively(Guid resultId, Guid secondFactorTransactionId, CancellationToken ct)
    {
        var result = await LoadPoliticalBusinessResult(resultId, true);
        if (!IsSelfOwnedPoliticalBusiness(result.MajorityElection))
        {
            throw new ValidationException("finish submission and audit tentatively is not allowed for a non self owned political business");
        }

        if (result.MajorityElection.DomainOfInfluence.Type < DataModels.DomainOfInfluenceType.Mu)
        {
            throw new ValidationException("finish submission and audit tentatively is not allowed for non communal political business");
        }

        await VerifySecondFactor<MajorityElectionResultAggregate>(
            result,
            nameof(SubmissionFinishedAndAuditedTentatively),
            secondFactorTransactionId,
            ct);

        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(resultId);
        await _validationResultsEnsurer.EnsureMajorityElectionResultIsValid(result);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(result.Id);
        aggregate.SubmissionFinished(contestId);
        aggregate.AuditedTentatively(contestId);
        _logger.LogInformation("Submission finished and audited tentatively for majority election result {MajorityElectionResultId}", result.Id);

        if (CanAutomaticallyPublishResults(result.MajorityElection.DomainOfInfluence, result.MajorityElection.Contest, aggregate))
        {
            aggregate.Publish(contestId);
            _logger.LogInformation("majority election result {MajorityElectionResultId} published", aggregate.Id);
        }

        await AggregateRepository.Save(aggregate);
    }

    public async Task<SecondFactorInfo> PrepareCorrectionFinishedAndAuditedTentatively(Guid resultId, string message)
    {
        return await PrepareSecondFactor<MajorityElectionResultAggregate>(nameof(CorrectionFinishedAndAuditedTentatively), resultId, message)
            ?? throw new InvalidOperationException("2FA is required in " + nameof(CorrectionFinishedAndAuditedTentatively));
    }

    public async Task CorrectionFinishedAndAuditedTentatively(Guid resultId, Guid secondFactorTransactionId, CancellationToken ct)
    {
        var result = await LoadPoliticalBusinessResult(resultId, true);
        if (!IsSelfOwnedPoliticalBusiness(result.MajorityElection))
        {
            throw new ValidationException("finish correction and audit tentatively is not allowed for a non self owned political business");
        }

        if (result.MajorityElection.DomainOfInfluence.Type < DataModels.DomainOfInfluenceType.Mu)
        {
            throw new ValidationException("finish correction and audit tentatively is not allowed for non communal political business");
        }

        await VerifySecondFactor<MajorityElectionResultAggregate>(
            result,
            nameof(CorrectionFinishedAndAuditedTentatively),
            secondFactorTransactionId,
            ct);

        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(resultId);
        await _validationResultsEnsurer.EnsureMajorityElectionResultIsValid(result);

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(result.Id);
        aggregate.CorrectionFinished(string.Empty, contestId);
        aggregate.AuditedTentatively(contestId);
        _logger.LogInformation("Correction finished and audited tentatively for majority election result {MajorityElectionResultId}", result.Id);

        if (CanAutomaticallyPublishResults(result.MajorityElection.DomainOfInfluence, result.MajorityElection.Contest, aggregate))
        {
            aggregate.Publish(contestId);
            _logger.LogInformation("majority election result {MajorityElectionResultId} published", aggregate.Id);
        }

        await AggregateRepository.Save(aggregate);
    }

    public async Task Publish(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<MajorityElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            var result = await LoadPoliticalBusinessResult(aggregate.Id);
            EnsureCanManuallyPublishResults(result.MajorityElection.Contest, result.MajorityElection.DomainOfInfluence, aggregate);
            aggregate.Publish(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} published", aggregate.Id);
        });
    }

    public async Task Unpublish(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<MajorityElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
            var result = await LoadPoliticalBusinessResult(aggregate.Id);
            EnsureCanManuallyPublishResults(result.MajorityElection.Contest, result.MajorityElection.DomainOfInfluence, aggregate);
            aggregate.Unpublish(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} unpublished", aggregate.Id);
        });
    }

    public async Task ResetToSubmissionFinishedAndFlagForCorrection(Guid resultId)
    {
        var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(resultId);
        var result = await LoadPoliticalBusinessResult(resultId);
        if (result.MajorityElection.DomainOfInfluence.Type < DataModels.DomainOfInfluenceType.Mu)
        {
            throw new ValidationException("reset to submission finished and flag for correction is not allowed for non communal political business");
        }

        var aggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(resultId);
        if (aggregate.Published)
        {
            aggregate.Unpublish(contestId);
            _logger.LogInformation("Majority election result {MajorityElectionResultId} unpublished", aggregate.Id);
        }

        aggregate.ResetToSubmissionFinished(contestId);
        aggregate.FlagForCorrection(contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Majority election result {MajorityElectionResultId} reset to submission finished and flagged for correction", aggregate.Id);
    }

    protected override Task<DataModels.MajorityElectionResult> LoadPoliticalBusinessResult(Guid resultId)
        => LoadPoliticalBusinessResult(resultId, false);

    private async Task<DataModels.MajorityElectionResult> LoadPoliticalBusinessResult(Guid resultId, bool includeCandidates)
    {
        IQueryable<DataModels.MajorityElectionResult> query = _resultRepo.Query()
                   .AsSplitQuery()
                   .Include(vr => vr.MajorityElection.Contest.CantonDefaults)
                   .Include(vr => vr.MajorityElection.DomainOfInfluence)
                   .Include(vr => vr.MajorityElection.Translations)
                   .Include(vr => vr.CountingCircle.ResponsibleAuthority)
                   .Include(vr => vr.CandidateResults)
                   .Include(vr => vr.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                   .Include(vr => vr.SecondaryMajorityElectionResults).ThenInclude(x => x.SecondaryMajorityElection.Translations)
                   .Include(vr => vr.BallotGroupResults);

        if (includeCandidates)
        {
            query = query.Include(vr => vr.SecondaryMajorityElectionResults)
                .ThenInclude(x => x.CandidateResults)
                .ThenInclude(x => x.Candidate);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == resultId)
               ?? throw new EntityNotFoundException(resultId);
    }

    private void EnsureResultEntryRespectSettings(
        DataModels.MajorityElection election,
        DataModels.MajorityElectionResultEntry resultEntry,
        MajorityElectionResultEntryParams? resultEntryParams)
    {
        if (election.EnforceResultEntryForCountingCircles
            && election.ResultEntry != resultEntry)
        {
            throw new ValidationException("enforced result entry setting not respected");
        }

        if (resultEntryParams != null
            && election.EnforceEmptyVoteCountingForCountingCircles
            && election.AutomaticEmptyVoteCounting != resultEntryParams.AutomaticEmptyVoteCounting)
        {
            throw new ValidationException(
                $"enforced {nameof(election.AutomaticEmptyVoteCounting)} setting not respected");
        }

        if (resultEntryParams != null
            && election.EnforceReviewProcedureForCountingCircles
            && election.ReviewProcedure != resultEntryParams.ReviewProcedure)
        {
            throw new ValidationException($"enforced {nameof(election.ReviewProcedure)} setting not respected");
        }

        if (resultEntryParams != null
            && election.EnforceCandidateCheckDigitForCountingCircles
            && election.CandidateCheckDigit != resultEntryParams.CandidateCheckDigit)
        {
            throw new ValidationException($"enforced {nameof(election.CandidateCheckDigit)} setting not respected");
        }
    }

    private void EnsureBallotGroupsExistsAndNoDuplicates(
        DataModels.MajorityElectionResult result,
        IReadOnlyCollection<MajorityElectionBallotGroupResult> providedResults)
    {
        if (providedResults.Select(x => x.BallotGroupId).Distinct().Count() != providedResults.Count)
        {
            throw new ValidationException("duplicated ballot groups provided");
        }

        var providedBallotGroupIds = providedResults.Select(x => x.BallotGroupId).ToHashSet();
        var hasUnmatchedBallotGroup = providedBallotGroupIds
            .Except(result.BallotGroupResults.Select(x => x.BallotGroupId))
            .Any();
        if (hasUnmatchedBallotGroup)
        {
            throw new ValidationException("ballot groups provided which don't exist");
        }

        if (result.BallotGroupResults.Any(r => !r.BallotGroup.AllCandidateCountsOk && providedBallotGroupIds.Contains(r.BallotGroupId)))
        {
            throw new ValidationException("provided results for a ballot group which isn't ready yet (candidate count is not ok).");
        }
    }

    private void EnsureCandidatesExistsAndNoDuplicates(
        DataModels.MajorityElectionResult electionResult,
        IReadOnlyCollection<MajorityElectionCandidateResult> candidateResults,
        IReadOnlyCollection<SecondaryMajorityElectionCandidateResults> secondaryCandidateResults)
    {
        if (candidateResults.Select(x => x.CandidateId).Distinct().Count() != candidateResults.Count)
        {
            throw new ValidationException("duplicated candidate provided");
        }

        var hasUnmatchedCandidates = candidateResults.Select(x => x.CandidateId)
            .Except(electionResult.CandidateResults.Select(cr => cr.CandidateId))
            .Any();
        if (hasUnmatchedCandidates)
        {
            throw new ValidationException("candidates provided which don't exist");
        }

        if (secondaryCandidateResults.Select(x => x.SecondaryMajorityElectionId).Distinct().Count() !=
            secondaryCandidateResults.Count)
        {
            throw new ValidationException("duplicated secondary election result provided");
        }

        var secondaryById = electionResult.SecondaryMajorityElectionResults.ToDictionary(x => x.SecondaryMajorityElectionId);
        foreach (var updatedSecondaryResult in secondaryCandidateResults)
        {
            if (!secondaryById.TryGetValue(updatedSecondaryResult.SecondaryMajorityElectionId, out var secondaryElectionResult))
            {
                throw new ValidationException("secondary election results provided which don't exist");
            }

            if (updatedSecondaryResult.CandidateResults.Select(c => c.CandidateId).Distinct().Count()
                != updatedSecondaryResult.CandidateResults.Count)
            {
                throw new ValidationException("duplicated candidate provided");
            }

            var hasUnmatchedSecondaryCandidates = updatedSecondaryResult.CandidateResults
                .Select(x => x.CandidateId)
                .Except(secondaryElectionResult.CandidateResults.Select(cr => cr.CandidateId))
                .Any();
            if (hasUnmatchedSecondaryCandidates)
            {
                throw new ValidationException("candidates provided which don't exists");
            }
        }
    }

    private void EnsureValidIndividualVoteCount(
        DataModels.MajorityElectionResult electionResult,
        int? individualVoteCount,
        IReadOnlyCollection<SecondaryMajorityElectionCandidateResults> secondaryCandidateResults)
    {
        if (electionResult.MajorityElection.IndividualCandidatesDisabled && individualVoteCount.HasValue)
        {
            throw new ValidationException($"Individual vote count is disabled on election {electionResult.MajorityElectionId}");
        }

        foreach (var secondaryCandidateResult in secondaryCandidateResults)
        {
            var secondaryElectionResult = electionResult.SecondaryMajorityElectionResults.FirstOrDefault(r => r.SecondaryMajorityElectionId == secondaryCandidateResult.SecondaryMajorityElectionId)
                ?? throw new EntityNotFoundException(secondaryCandidateResult.SecondaryMajorityElectionId);

            if (secondaryElectionResult.SecondaryMajorityElection.IndividualCandidatesDisabled && secondaryCandidateResult.IndividualVoteCount.HasValue)
            {
                throw new ValidationException($"Individual vote count is disabled on secondary election {secondaryElectionResult.SecondaryMajorityElectionId}");
            }
        }
    }

    private void EnsureNoEmptyVoteCountForSingleMandate(DataModels.MajorityElectionResult electionResult, int? emptyVoteCount)
    {
        if (electionResult.Entry != DataModels.MajorityElectionResultEntry.FinalResults)
        {
            return;
        }

        if (electionResult.MajorityElection.NumberOfMandates == 1 && emptyVoteCount != null && !electionResult.SecondaryMajorityElectionResults.Any())
        {
            throw new ValidationException("empty vote count provided with single mandate and no other elections on the same ballot");
        }
    }
}
