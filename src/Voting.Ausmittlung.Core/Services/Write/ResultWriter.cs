// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ResultWriter
{
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IDbRepository<DataContext, Vote> _voteRepository;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepository;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepository;
    private readonly PermissionService _permissionService;
    private readonly ContestService _contestService;
    private readonly ResultReader _resultReader;
    private readonly SecondFactorTransactionWriter _secondFactorTransactionWriter;
    private readonly ValidationResultsEnsurer _validationResultsEnsurer;
    private readonly CountingCircleResultsValidationSummariesBuilder _countingCircleResultsValidationSummariesBuilder;
    private readonly IAuth _auth;

    public ResultWriter(
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        PermissionService permissionService,
        IDbRepository<DataContext, Vote> voteRepository,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepository,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepository,
        ContestService contestService,
        ResultReader resultReader,
        SecondFactorTransactionWriter secondFactorTransactionWriter,
        ValidationResultsEnsurer validationResultsEnsurer,
        CountingCircleResultsValidationSummariesBuilder countingCircleResultsValidationSummariesBuilder,
        IAuth auth)
    {
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _permissionService = permissionService;
        _voteRepository = voteRepository;
        _majorityElectionRepository = majorityElectionRepository;
        _simplePoliticalBusinessRepository = simplePoliticalBusinessRepository;
        _contestService = contestService;
        _resultReader = resultReader;
        _secondFactorTransactionWriter = secondFactorTransactionWriter;
        _validationResultsEnsurer = validationResultsEnsurer;
        _countingCircleResultsValidationSummariesBuilder = countingCircleResultsValidationSummariesBuilder;
        _auth = auth;
    }

    public async Task StartSubmission(ResultList data)
    {
        // Only start the submission for the correct states and users
        if (data.Contest.State.IsLocked()
            || !_permissionService.IsErfassungElectionAdmin()
            || !(data.CountingCircle.ResponsibleAuthority.SecureConnectId.Equals(_permissionService.TenantId, StringComparison.Ordinal)
                 || (data.Contest.DomainOfInfluence.SecureConnectId.Equals(_permissionService.TenantId, StringComparison.Ordinal) && !data.Contest.TestingPhaseEnded)))
        {
            return;
        }

        var results = data.Results
            .Where(r => r.State == CountingCircleResultState.Initial)
            .GroupBy(x => x.PoliticalBusiness!.BusinessType);

        foreach (var groupedResults in results)
        {
            switch (groupedResults.Key)
            {
                case PoliticalBusinessType.Vote:
                    await StartVoteSubmission(data.CountingCircle, groupedResults.ToList(), data.Contest.TestingPhaseEnded);
                    break;
                case PoliticalBusinessType.ProportionalElection:
                    await StartProportionalElectionSubmission(data.CountingCircle, groupedResults, data.Contest.TestingPhaseEnded);
                    break;
                case PoliticalBusinessType.MajorityElection:
                    await StartMajorityElectionSubmission(data.CountingCircle, groupedResults.ToList(), data.Contest.TestingPhaseEnded);
                    break;
            }
        }
    }

    public async Task ResetResults(Guid contestId, Guid countingCircleId)
    {
        var data = await _resultReader.GetList(contestId, countingCircleId);

        if (data.Details == null)
        {
            throw new ValidationException("Counting circle details is not initialized yet");
        }

        _permissionService.EnsureErfassungElectionAdmin();
        _contestService.EnsureInTestingPhase(data.Contest);
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(countingCircleId, contestId);

        var ccDetailsAggregate = await _aggregateRepository.TryGetById<ContestCountingCircleDetailsAggregate>(data.Details.Id)
            ?? throw new ValidationException("Counting circle details aggregate is not initialized yet");

        if (data.Results.Any(r => r.State <= CountingCircleResultState.Initial))
        {
            throw new ValidationException("Cannot reset results when there are any initial results");
        }

        if (data.Results.Any(r => r.State > CountingCircleResultState.CorrectionDone))
        {
            throw new ValidationException("Cannot reset results when there are any audited or plausibilised results");
        }

        var resultAggregates = await GetResultAggregates(data.Results);

        // Apply the action on the aggregates first to ensure that the aggregate state is valid.
        ccDetailsAggregate.Reset();

        foreach (var resultAggregate in resultAggregates)
        {
            resultAggregate.Reset(contestId);
        }

        await _aggregateRepository.Save(ccDetailsAggregate);

        foreach (var resultAggregate in resultAggregates)
        {
            await _aggregateRepository.Save(resultAggregate);
        }
    }

    public async Task<(SecondFactorTransaction? SecondFactorTransaction, string? Code)> PrepareSubmissionFinished(
        Guid contestId,
        Guid countingCircleId,
        IReadOnlyCollection<Guid> resultIds,
        string message)
    {
        var data = await _resultReader.GetList(contestId, countingCircleId);
        if (data.Details == null)
        {
            throw new ValidationException("Counting circle details is not initialized yet");
        }

        _permissionService.EnsureErfassungElectionAdmin();
        _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircle(data.CountingCircle, data.Contest);
        _contestService.EnsureNotLocked(data.Contest);

        var ccDetailsAggregate = await _aggregateRepository.TryGetById<ContestCountingCircleDetailsAggregate>(data.Details.Id)
            ?? throw new ValidationException("Counting circle details aggregate is not initialized yet");
        var ccResultAggregates = await GetResultAggregatesForFinishSubmission(data.Results, resultIds);

        var actionAggregates = new List<BaseEventSourcingAggregate> { ccDetailsAggregate };
        actionAggregates.AddRange(ccResultAggregates);

        var pbIds = ccResultAggregates.Select(x => x.PoliticalBusinessId).ToList();
        var hasOnlySelfOwnedPoliticalBusinesses = await HasOnlySelfOwnedPoliticalBusinesses(pbIds);

        if (hasOnlySelfOwnedPoliticalBusinesses)
        {
            return default;
        }

        var actionId = new ActionId(nameof(SubmissionFinished), actionAggregates.ToArray());
        return await _secondFactorTransactionWriter.CreateSecondFactorTransaction(actionId, message);
    }

    public async Task SubmissionFinished(
        Guid contestId,
        Guid countingCircleId,
        IReadOnlyCollection<Guid> resultIds,
        string secondFactorTransactionExternalId,
        CancellationToken ct)
    {
        var data = await _resultReader.GetList(contestId, countingCircleId);
        if (data.Details == null)
        {
            throw new ValidationException("Counting circle details is not initialized yet");
        }

        // needed for permission check on validation results building.
        data.Details.Contest = data.Contest;
        data.Details.CountingCircle = data.CountingCircle;

        var validationSummaries = await _countingCircleResultsValidationSummariesBuilder.BuildValidationSummaries(data.Details, resultIds);

        foreach (var validationSummary in validationSummaries)
        {
            _validationResultsEnsurer.EnsureIsValid(validationSummary.ValidationResults);
        }

        var ccDetailsAggregate = await _aggregateRepository.TryGetById<ContestCountingCircleDetailsAggregate>(data.Details.Id)
            ?? throw new ValidationException("Counting circle details aggregate is not initialized yet");
        var ccResultAggregates = await GetResultAggregatesForFinishSubmission(data.Results, resultIds);

        var actionAggregates = new List<BaseEventSourcingAggregate> { ccDetailsAggregate };
        actionAggregates.AddRange(ccResultAggregates);

        var pbIds = ccResultAggregates.Select(x => x.PoliticalBusinessId).ToList();
        var hasOnlySelfOwnedPoliticalBusinesses = await HasOnlySelfOwnedPoliticalBusinesses(pbIds);

        if (!hasOnlySelfOwnedPoliticalBusinesses)
        {
            await _secondFactorTransactionWriter.EnsureVerified(
                secondFactorTransactionExternalId,
                () => Task.FromResult(new ActionId(nameof(SubmissionFinished), actionAggregates.ToArray())),
                ct);
        }

        // apply the events first to ensure that all aggregates are in a correct state.
        foreach (var ccResultAggregate in ccResultAggregates)
        {
            ccResultAggregate.SubmissionFinished(contestId);
        }

        foreach (var ccResultAggregate in ccResultAggregates)
        {
            await _aggregateRepository.Save(ccResultAggregate);
        }
    }

    private async Task StartVoteSubmission(CountingCircle countingCircle, IReadOnlyCollection<SimpleCountingCircleResult> results, bool testingPhaseEnded)
    {
        var ids = results.Select(x => x.PoliticalBusinessId).ToList();

        // if the result entry is enforced and only final results should be provided, it should be defined automatically
        var idsWithEnforcedFinalResultEntryList = await _voteRepository.Query()
            .Where(x => x.EnforceResultEntryForCountingCircles && x.ResultEntry == VoteResultEntry.FinalResults && ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();
        var idsWithEnforcedFinalResultEntry = idsWithEnforcedFinalResultEntryList.ToHashSet();

        foreach (var result in results)
        {
            result.State = CountingCircleResultState.SubmissionOngoing;

            var aggregate = _aggregateFactory.New<VoteResultAggregate>();
            aggregate.StartSubmission(countingCircle.BasisCountingCircleId, result.PoliticalBusinessId, result.PoliticalBusiness!.ContestId, testingPhaseEnded);

            if (idsWithEnforcedFinalResultEntry.Contains(result.PoliticalBusinessId))
            {
                aggregate.DefineEntry(VoteResultEntry.FinalResults, result.PoliticalBusiness.ContestId);
            }

            await _aggregateRepository.Save(aggregate);
        }
    }

    private async Task StartProportionalElectionSubmission(CountingCircle countingCircle, IEnumerable<SimpleCountingCircleResult> results, bool testingPhaseEnded)
    {
        foreach (var result in results)
        {
            result.State = CountingCircleResultState.SubmissionOngoing;

            var aggregate = _aggregateFactory.New<ProportionalElectionResultAggregate>();
            aggregate.StartSubmission(countingCircle.BasisCountingCircleId, result.PoliticalBusinessId, result.PoliticalBusiness!.ContestId, testingPhaseEnded);
            await _aggregateRepository.Save(aggregate);
        }
    }

    private async Task StartMajorityElectionSubmission(CountingCircle countingCircle, IReadOnlyCollection<SimpleCountingCircleResult> results, bool testingPhaseEnded)
    {
        var ids = results.Select(x => x.PoliticalBusinessId).ToList();

        // if the result entry is enforced and only final results should be provided, it should be defined automatically
        var idsWithEnforcedFinalResultEntryList = await _majorityElectionRepository.Query()
            .Where(x => x.EnforceResultEntryForCountingCircles && x.ResultEntry == MajorityElectionResultEntry.FinalResults && ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();
        var idsWithEnforcedFinalResultEntry = idsWithEnforcedFinalResultEntryList.ToHashSet();

        foreach (var result in results)
        {
            result.State = CountingCircleResultState.SubmissionOngoing;

            var aggregate = _aggregateFactory.New<MajorityElectionResultAggregate>();
            aggregate.StartSubmission(countingCircle.BasisCountingCircleId, result.PoliticalBusinessId, result.PoliticalBusiness!.ContestId, testingPhaseEnded);

            // if the result entry is enforced and only final results should be provided, it should be defined automatically
            if (idsWithEnforcedFinalResultEntry.Contains(result.PoliticalBusinessId))
            {
                aggregate.DefineEntry(MajorityElectionResultEntry.FinalResults, result.PoliticalBusiness.ContestId);
            }

            await _aggregateRepository.Save(aggregate);
        }
    }

    private async Task<IReadOnlyCollection<CountingCircleResultAggregate>> GetResultAggregates(IReadOnlyCollection<SimpleCountingCircleResult> countingCircleResults)
    {
        var results = countingCircleResults
            .GroupBy(x => x.PoliticalBusiness!.BusinessType)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToList());
        var resultIds = countingCircleResults.Select(r => r.Id).ToList();

        var aggregates = new List<CountingCircleResultAggregate>();
        aggregates.AddRange((await GetAggregates<VoteResultAggregate>(results.GetValueOrDefault(PoliticalBusinessType.Vote) ?? new())).ToList());
        aggregates.AddRange((await GetAggregates<ProportionalElectionResultAggregate>(results.GetValueOrDefault(PoliticalBusinessType.ProportionalElection) ?? new())).ToList());
        aggregates.AddRange((await GetAggregates<MajorityElectionResultAggregate>(results.GetValueOrDefault(PoliticalBusinessType.MajorityElection) ?? new())).ToList());
        return aggregates.OrderBy(r => resultIds.IndexOf(r.Id)).ToList();
    }

    private async Task<IReadOnlyCollection<TAggregate>> GetAggregates<TAggregate>(IReadOnlyCollection<Guid> ids)
        where TAggregate : BaseEventSourcingAggregate
    {
        if (ids.Distinct().Count() != ids.Count)
        {
            throw new ValidationException("duplicate ids present");
        }

        var aggregates = new List<TAggregate>();
        foreach (var id in ids)
        {
            var aggregate = await _aggregateRepository.GetById<TAggregate>(id);
            aggregates.Add(aggregate);
        }

        return aggregates;
    }

    private async Task<IReadOnlyCollection<CountingCircleResultAggregate>> GetResultAggregatesForFinishSubmission(
        IReadOnlyCollection<SimpleCountingCircleResult> simpleCcResults,
        IReadOnlyCollection<Guid> resultIds)
    {
        var results = simpleCcResults.Where(r => resultIds.Contains(r.Id)).ToList();

        if (results.Count != resultIds.Count)
        {
            throw new ValidationException("Non existing result id provided");
        }

        if (results.Any(r => r.State != CountingCircleResultState.SubmissionOngoing))
        {
            throw new ValidationException("Cannot finish submission when there are results which are not with ongoing submission");
        }

        return await GetResultAggregates(results);
    }

    private async Task<bool> HasOnlySelfOwnedPoliticalBusinesses(IReadOnlyCollection<Guid> pbIds)
    {
        return await _simplePoliticalBusinessRepository
            .Query()
            .Include(x => x.DomainOfInfluence)
            .Where(x => pbIds.Contains(x.Id))
            .AllAsync(x => x.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id);
    }
}
