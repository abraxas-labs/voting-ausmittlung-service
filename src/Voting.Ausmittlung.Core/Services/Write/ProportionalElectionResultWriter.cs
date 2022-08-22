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
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ProportionalElectionResultWriter : PoliticalBusinessResultWriter<DataModels.ProportionalElectionResult>
{
    private readonly ILogger<ProportionalElectionResultWriter> _logger;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionResult> _resultRepo;
    private readonly PermissionService _permissionService;
    private readonly ValidationResultsEnsurer _validationResultsEnsurer;
    private readonly SecondFactorTransactionWriter _secondFactorTransactionWriter;

    public ProportionalElectionResultWriter(
        ILogger<ProportionalElectionResultWriter> logger,
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, DataModels.ProportionalElectionResult> resultRepo,
        PermissionService permissionService,
        ContestService contestService,
        ValidationResultsEnsurer validationResultsEnsurer,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
        : base(permissionService, contestService, aggregateRepository)
    {
        _logger = logger;
        _resultRepo = resultRepo;
        _permissionService = permissionService;
        _validationResultsEnsurer = validationResultsEnsurer;
        _secondFactorTransactionWriter = secondFactorTransactionWriter;
    }

    public async Task DefineEntry(Guid resultId, ElectionResultEntryParams resultEntryParams)
    {
        _permissionService.EnsureErfassungElectionAdmin();
        var result = await LoadPoliticalBusinessResult(resultId);
        var contestId = await EnsurePoliticalBusinessPermissions(result, true);
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
        var contestId = await EnsurePoliticalBusinessPermissions(electionResult, true);

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
        var contestId = await EnsurePoliticalBusinessPermissions(electionResult, true);
        EnsureListsExists(electionResult, results);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        aggregate.EnterUnmodifiedListResults(results, contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Entered unmodified list results for proportional election result {ProportionalElectionResultId}", resultId);
    }

    public async Task<string> PrepareSubmissionFinished(Guid resultId, string message)
    {
        await EnsurePoliticalBusinessPermissions(resultId, true);

        var actionId = await PrepareSubmissionFinishedActionId(resultId);
        var secondFactorTransaction = await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
        return secondFactorTransaction.ExternalIdentifier;
    }

    public async Task SubmissionFinished(Guid resultId, string secondFactorTransactionExternalId, CancellationToken ct)
    {
        var result = await LoadPoliticalBusinessResult(resultId);

        var contestId = await EnsurePoliticalBusinessPermissions(result, true);
        await _secondFactorTransactionWriter.EnsureVerified(secondFactorTransactionExternalId, () => PrepareSubmissionFinishedActionId(result.Id), ct);
        await _validationResultsEnsurer.EnsureProportionalElectionResultIsValid(result);

        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(result.Id);
        aggregate.SubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
        _logger.LogInformation("Submission finished for proportional election result {ProportionalElectionResultId}", result.Id);
    }

    public async Task<string> PrepareCorrectionFinished(Guid resultId, string message)
    {
        await EnsurePoliticalBusinessPermissions(resultId, true);

        var actionId = await PrepareCorrectionFinishedActionId(resultId);
        var secondFactorTransaction = await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
        return secondFactorTransaction.ExternalIdentifier;
    }

    public async Task CorrectionFinished(Guid resultId, string comment, string secondFactorTransactionExternalId, CancellationToken ct)
    {
        var result = await LoadPoliticalBusinessResult(resultId);

        var contestId = await EnsurePoliticalBusinessPermissions(result, true);
        await _secondFactorTransactionWriter.EnsureVerified(secondFactorTransactionExternalId, () => PrepareCorrectionFinishedActionId(result.Id), ct);
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
        });
    }

    public async Task Plausibilise(IReadOnlyCollection<Guid> resultIds)
    {
        await ExecuteOnAllAggregates<ProportionalElectionResultAggregate>(resultIds, async aggregate =>
        {
            var contestId = await EnsurePoliticalBusinessPermissionsForMonitor(aggregate.Id);
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

    protected override async Task<DataModels.ProportionalElectionResult> LoadPoliticalBusinessResult(Guid resultId)
    {
        return await _resultRepo.Query()
                   .Include(vr => vr.ProportionalElection.Contest)
                   .Include(vr => vr.ProportionalElection.DomainOfInfluence)
                   .Include(vr => vr.CountingCircle.ResponsibleAuthority)
                   .FirstOrDefaultAsync(x => x.Id == resultId)
               ?? throw new EntityNotFoundException(resultId);
    }

    private void EnsureValidResultEntry(DataModels.ProportionalElection election, ElectionResultEntryParams resultEntryParams)
    {
        if (election.EnforceEmptyVoteCountingForCountingCircles
            && election.AutomaticEmptyVoteCounting != resultEntryParams.AutomaticEmptyVoteCounting)
        {
            throw new ValidationException($"enforced {nameof(election.AutomaticEmptyVoteCounting)} setting not respected");
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

    private async Task<ActionId> PrepareSubmissionFinishedActionId(Guid resultId)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        return aggregate.PrepareSubmissionFinished();
    }

    private async Task<ActionId> PrepareCorrectionFinishedActionId(Guid resultId)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        return aggregate.PrepareCorrectionFinished();
    }
}
