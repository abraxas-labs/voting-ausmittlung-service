// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ProportionalElectionEndResultWriter : ElectionEndResultWriter<
    ProportionalElectionEndResultAvailableLotDecision,
    DataModels.ProportionalElectionCandidate,
    ProportionalElectionEndResultAggregate,
    DataModels.ProportionalElectionEndResult>
{
    private readonly ProportionalElectionEndResultReader _endResultReader;
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionEndResult> _endResultRepo;
    private readonly ProportionalElectionRepo _proportionalElectionRepo;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionListEndResult> _listEndResultRepo;

    public ProportionalElectionEndResultWriter(
        ILogger<ProportionalElectionEndResultWriter> logger,
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        ProportionalElectionEndResultReader endResultReader,
        ContestService contestService,
        PermissionService permissionService,
        IDbRepository<DataContext, DataModels.ProportionalElectionEndResult> endResultRepo,
        SecondFactorTransactionWriter secondFactorTransactionWriter,
        IDbRepository<DataContext, DataModels.ProportionalElectionListEndResult> listEndResultRepo,
        ProportionalElectionRepo proportionalElectionRepo)
        : base(logger, aggregateRepository, aggregateFactory, contestService, permissionService, secondFactorTransactionWriter)
    {
        _endResultReader = endResultReader;
        _permissionService = permissionService;
        _endResultRepo = endResultRepo;
        _listEndResultRepo = listEndResultRepo;
        _proportionalElectionRepo = proportionalElectionRepo;
    }

    public async Task UpdateEndResultLotDecisions(
        Guid proportionalElectionListId,
        IReadOnlyCollection<ElectionEndResultLotDecision> lotDecisions)
    {
        var availableLotDecisions = await _endResultReader.GetEndResultAvailableLotDecisions(proportionalElectionListId);
        var proportionalElectionEndResultId = availableLotDecisions.ProportionalElectionEndResultId;

        var contest = await _endResultRepo.Query()
                          .Where(x => x.Id == proportionalElectionEndResultId)
                          .Select(x => x.ProportionalElection.Contest)
                          .FirstOrDefaultAsync()
                      ?? throw new EntityNotFoundException(proportionalElectionEndResultId);
        ContestService.EnsureNotLocked(contest);

        ValidateLotDecisions(lotDecisions, availableLotDecisions);

        var endResultAggregate = await AggregateRepository.GetOrCreateById<ProportionalElectionEndResultAggregate>(proportionalElectionEndResultId);
        endResultAggregate.UpdateLotDecisions(
            availableLotDecisions.ProportionalElectionList.ProportionalElectionId,
            availableLotDecisions.ProportionalElectionList.Id,
            lotDecisions,
            contest.Id,
            contest.TestingPhaseEnded);

        await AggregateRepository.Save(endResultAggregate);
        Logger.LogInformation(
            "Updated lot decisions for proportional election end result {ProportionalElectionEndResultId}",
            proportionalElectionEndResultId);
    }

    public async Task EnterManualListEndResult(Guid listId, IReadOnlyCollection<ProportionalElectionManualCandidateEndResult> candidateEndResults)
    {
        var listEndResult = await _listEndResultRepo.Query()
            .Include(l => l.CandidateEndResults)
            .Include(l => l.ElectionEndResult.ProportionalElection.Contest)
            .FirstOrDefaultAsync(l => l.ListId == listId && l.ElectionEndResult.ProportionalElection.DomainOfInfluence.SecureConnectId == _permissionService.TenantId)
            ?? throw new EntityNotFoundException(listId);

        var contest = listEndResult.ElectionEndResult.ProportionalElection.Contest;
        ContestService.EnsureNotLocked(contest);

        if (!listEndResult.ElectionEndResult.ManualEndResultRequired)
        {
            throw new ForbiddenException($"Cannot enter a manual end result for election {listEndResult.ElectionEndResult.ProportionalElectionId}");
        }

        ValidateManualCandidateEndResults(listEndResult.CandidateEndResults, candidateEndResults);

        var endResultAggregate = await AggregateRepository.GetOrCreateById<ProportionalElectionEndResultAggregate>(listEndResult.ElectionEndResultId);
        endResultAggregate.EnterManualListEndResult(
            listEndResult.ElectionEndResult.ProportionalElectionId,
            listId,
            candidateEndResults,
            contest.Id,
            contest.TestingPhaseEnded);

        await AggregateRepository.Save(endResultAggregate);
    }

    public async Task StartMandateDistribution(Guid proportionalElectionId)
    {
        var endResult = await GetEndResult(proportionalElectionId, _permissionService.TenantId)
            ?? throw new EntityNotFoundException(proportionalElectionId);
        var (contestId, testingPhaseEnded) = await ContestService.EnsureNotLocked(endResult.ProportionalElection.ContestId);

        ValidateStartMandateDistribution(endResult);

        var endResultAggregate = await AggregateRepository.GetOrCreateById<ProportionalElectionEndResultAggregate>(endResult.Id);
        endResultAggregate.StartMandateDistribution(proportionalElectionId, contestId, testingPhaseEnded);
        await AggregateRepository.Save(endResultAggregate);
    }

    public async Task RevertMandateDistribution(Guid proportionalElectionId)
    {
        var endResult = await GetEndResult(proportionalElectionId, _permissionService.TenantId)
            ?? throw new EntityNotFoundException(proportionalElectionId);
        var (contestId, _) = await ContestService.EnsureNotLocked(endResult.ProportionalElection.ContestId);

        if (!endResult.MandateDistributionTriggered)
        {
            throw new ValidationException("Cannot revert mandate distribution, if it is not triggered yet.");
        }

        var endResultAggregate = await AggregateRepository.GetById<ProportionalElectionEndResultAggregate>(endResult.Id);
        endResultAggregate.RevertMandateDistribution(contestId);
        await AggregateRepository.Save(endResultAggregate);
    }

    public async Task UpdateEndResultListLotDecisions(
        Guid proportionalElectionId,
        List<DataModels.ProportionalElectionEndResultListLotDecision> listLotDecisions)
    {
        var endResult = await _endResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ProportionalElection.Contest)
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.List.ProportionalElectionListUnionEntries)
            .ThenInclude(x => x.ProportionalElectionListUnion)
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == proportionalElectionId && x.ProportionalElection.DomainOfInfluence.SecureConnectId == _permissionService.TenantId)
            ?? throw new EntityNotFoundException(proportionalElectionId);

        var contest = endResult.ProportionalElection.Contest;
        ContestService.EnsureNotLocked(contest);

        if (!endResult.AllCountingCirclesDone || !endResult.MandateDistributionTriggered)
        {
            throw new ValidationException("lot decisions are not allowed on this end result");
        }

        if (!endResult.ManualEndResultRequired)
        {
            throw new ValidationException("list lot decisions not allowed for non manual end result required");
        }

        var listIds = endResult.ListEndResults.Select(x => x.ListId);
        var listUnionIds = endResult.ListEndResults.Where(x => x.List.ProportionalElectionListUnion != null)
            .Select(x => x.List.ProportionalElectionListUnion!.Id);
        var listSubUnionIds = endResult.ListEndResults.Where(x => x.List.ProportionalElectionSubListUnion != null)
            .Select(x => x.List.ProportionalElectionSubListUnion!.Id);

        var availableListIds = listIds.Concat(listUnionIds.Concat(listSubUnionIds)).ToList();
        ValidateListLotDecisions(availableListIds, listLotDecisions);

        var endResultAggregate = await AggregateRepository.GetOrCreateById<ProportionalElectionEndResultAggregate>(endResult.Id);
        endResultAggregate.UpdateListLotDecisions(
            proportionalElectionId,
            listLotDecisions,
            contest.Id,
            contest.TestingPhaseEnded);

        await AggregateRepository.Save(endResultAggregate);
        Logger.LogInformation(
            "Updated list lot decisions for proportional election end result {ProportionalElectionEndResultId}",
            endResult.Id);
    }

    protected override Task<DataModels.ProportionalElectionEndResult?> GetEndResult(Guid politicalBusinessId, string tenantId)
    {
        return _endResultRepo.Query()
            .Include(x => x.ProportionalElection)
            .FirstOrDefaultAsync(x =>
                x.ProportionalElectionId == politicalBusinessId &&
                x.ProportionalElection.DomainOfInfluence.SecureConnectId == tenantId);
    }

    protected override async Task ValidateFinalize(DataModels.ProportionalElectionEndResult endResult, Guid contestId)
    {
        await base.ValidateFinalize(endResult, contestId);

        if (!endResult.MandateDistributionTriggered)
        {
            throw new ValidationException("Cannot finalize when mandates are not distributed yet");
        }

        if (!endResult.ProportionalElection.MandateAlgorithm.IsDoubleProportional())
        {
            return;
        }

        var election = await _proportionalElectionRepo.Query()
            .Include(pe => pe.DoubleProportionalResult)
            .Include(pe => pe.ProportionalElectionUnionEntries)
                .ThenInclude(e => e.ProportionalElectionUnion.DoubleProportionalResult)
            .FirstAsync(pe => pe.Id == endResult.ProportionalElectionId);

        var dpResults = new[] { election.DoubleProportionalResult }.Concat(election.ProportionalElectionUnionEntries.Select(e => e.ProportionalElectionUnion.DoubleProportionalResult))
            .WhereNotNull()
            .ToList();

        if (dpResults.Count == 0)
        {
            throw new ValidationException("finalization is not possible if the double proportional election result is not calculated");
        }

        if (dpResults.Any(x => !x.AllNumberOfMandatesDistributed))
        {
            throw new ValidationException("finalization is only possible if the double proportional election result distributed all number of mandates");
        }
    }

    protected override async Task ValidateRevertFinalize(DataModels.ProportionalElectionEndResult endResult, Guid contestId)
    {
        await base.ValidateRevertFinalize(endResult, contestId);

        if (!endResult.MandateDistributionTriggered)
        {
            throw new ValidationException("Cannot revert finalization if mandate distribution is not triggered yet");
        }
    }

    private void ValidateStartMandateDistribution(DataModels.ProportionalElectionEndResult endResult)
    {
        if (!endResult.AllCountingCirclesDone)
        {
            throw new ValidationException("Not all counting circles are done");
        }

        if (endResult.MandateDistributionTriggered)
        {
            throw new ValidationException("Cannot start mandate distribution, if it is already triggered");
        }

        if (endResult.ProportionalElection.MandateAlgorithm.IsUnionDoubleProportional())
        {
            throw new ValidationException("Cannot start mandate distribution with a union mandate algorithm");
        }
    }

    private void ValidateLotDecisions(
        IReadOnlyCollection<ElectionEndResultLotDecision> lotDecisions,
        ProportionalElectionListEndResultAvailableLotDecisions availableLotDecisions)
    {
        EnsureValidCandidates(
            lotDecisions,
            availableLotDecisions.LotDecisions);

        EnsureValidRanksInLotDecisions(
            lotDecisions,
            availableLotDecisions.LotDecisions);
    }

    private void ValidateManualCandidateEndResults(
        IEnumerable<DataModels.ProportionalElectionCandidateEndResult> candidateEndResults,
        IReadOnlyCollection<ProportionalElectionManualCandidateEndResult> manualCandidateEndResults)
    {
        if (manualCandidateEndResults.Count != manualCandidateEndResults.DistinctBy(x => x.CandidateId).Count())
        {
            throw new ValidationException("No candidate duplicates allowed");
        }

        if (candidateEndResults.Count() != manualCandidateEndResults.Count)
        {
            throw new ValidationException("All candidate end results of a list must be provided");
        }

        var candidateIds = candidateEndResults.Select(c => c.CandidateId).ToHashSet();
        var validCandidateEndResultStates = new HashSet<DataModels.ProportionalElectionCandidateEndResultState>
        {
            DataModels.ProportionalElectionCandidateEndResultState.Elected,
            DataModels.ProportionalElectionCandidateEndResultState.NotElected,
        };

        foreach (var manualCandidateEndResult in manualCandidateEndResults)
        {
            if (!candidateIds.Contains(manualCandidateEndResult.CandidateId))
            {
                throw new ValidationException("All candidate end results of a list must be provided");
            }

            if (!validCandidateEndResultStates.Contains(manualCandidateEndResult.State))
            {
                throw new ValidationException("Invalid candidate end result state");
            }
        }
    }

    private void ValidateListLotDecisions(
        IReadOnlyCollection<Guid> availableListIds,
        IReadOnlyCollection<DataModels.ProportionalElectionEndResultListLotDecision> listLotDecisions)
    {
        foreach (var listLotDecision in listLotDecisions)
        {
            if (listLotDecision.Entries.Count < 2)
            {
                throw new ValidationException("a list lot decision must have at least 2 entries");
            }

            if (!listLotDecision.Entries.Any(x => x.Winning))
            {
                throw new ValidationException("a list lot decision must have at least 1 winning entry");
            }

            if (listLotDecision.Entries.Any(entry => entry.ListId == null && entry.ListUnionId == null))
            {
                throw new ValidationException("a list lot decision entry must have a list id or list union id");
            }

            if (listLotDecision.Entries.Any(entry => entry.ListId != null && entry.ListUnionId != null))
            {
                throw new ValidationException("a list lot decision entry can only have a list id or list union id");
            }

            if (listLotDecision.Entries.Count != listLotDecision.Entries.Select(x => x.ListId ?? x.ListUnionId).Distinct().Count())
            {
                throw new ValidationException("a list id or list union id may only appear once in the lot decisions");
            }

            if (listLotDecision.Entries.Any(x => !availableListIds.Contains(x.ListId ?? x.ListUnionId!.Value)))
            {
                throw new ValidationException("a list id or list union id found which not exists in available lists");
            }
        }
    }
}
