// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class MajorityElectionResultBundleWriter
    : PoliticalBusinessResultBundleWriter<DataModels.MajorityElectionResult, MajorityElectionResultBundleAggregate>
{
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionResultBundle> _bundleRepo;

    public MajorityElectionResultBundleWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        IDbRepository<DataContext, DataModels.MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, DataModels.MajorityElectionResultBundle> bundleRepo,
        PermissionService permissionService,
        ContestService contestService,
        IAuth auth)
        : base(permissionService, contestService, auth, aggregateRepository)
    {
        _aggregateFactory = aggregateFactory;
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
    }

    public async Task<MajorityElectionResultBundleAggregate> CreateBundle(
        Guid resultId,
        int? bundleNumber)
    {
        var contestId = await EnsurePoliticalBusinessPermissions(resultId, false);

        var electionResultAggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(resultId);

        var generateBundleNumber = electionResultAggregate.ResultEntryParams.AutomaticBallotBundleNumberGeneration;
        if (generateBundleNumber)
        {
            bundleNumber = electionResultAggregate.GenerateBundleNumber(contestId);
        }
        else if (bundleNumber.HasValue)
        {
            electionResultAggregate.BundleNumberEntered(bundleNumber.Value, contestId);
        }
        else
        {
            throw new ValidationException("bundle number is not generated automatically and should be provided");
        }

        var aggregate = _aggregateFactory.New<MajorityElectionResultBundleAggregate>();
        aggregate.Create(
            null,
            resultId,
            bundleNumber.Value,
            electionResultAggregate.ResultEntry,
            electionResultAggregate.ResultEntryParams,
            contestId);

        await AggregateRepository.Save(electionResultAggregate);
        await AggregateRepository.Save(aggregate);
        return aggregate;
    }

    public async Task DeleteBundle(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate, true);
        aggregate.Delete(contestId);

        var electionResultAggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(aggregate.PoliticalBusinessResultId);
        electionResultAggregate.FreeBundleNumber(aggregate.BundleNumber, contestId);

        await AggregateRepository.Save(electionResultAggregate);
        await AggregateRepository.Save(aggregate);
    }

    public async Task<int> CreateBallot(
        Guid bundleId,
        int? emptyVoteCount,
        int individualVoteCount,
        int invalidVoteCount,
        IReadOnlyCollection<Guid> selectedCandidateIds,
        IReadOnlyCollection<SecondaryMajorityElectionResultBallot> secondaryResults)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var electionResult = await LoadElectionResultWithCandidates(aggregate.PoliticalBusinessResultId);

        var contestId = await EnsureEditPermissionsForBundle(electionResult, aggregate, false);
        EnsureValidCandidates(electionResult, selectedCandidateIds);

        emptyVoteCount = CalculateAndValidateEmptyVoteCount(
            emptyVoteCount,
            electionResult.MajorityElection.NumberOfMandates,
            individualVoteCount,
            invalidVoteCount,
            aggregate.ResultEntryParams.AutomaticEmptyVoteCounting,
            selectedCandidateIds.Count);

        ValidateSecondaryResults(electionResult, secondaryResults);

        aggregate.CreateBallot(emptyVoteCount.Value, individualVoteCount, invalidVoteCount, selectedCandidateIds, secondaryResults, contestId);
        await AggregateRepository.Save(aggregate);
        return aggregate.CurrentBallotNumber;
    }

    public async Task UpdateBallot(
        Guid bundleId,
        int ballotNumber,
        int? emptyVoteCount,
        int individualVoteCount,
        int invalidVoteCount,
        IReadOnlyCollection<Guid> selectedCandidateIds,
        IReadOnlyCollection<SecondaryMajorityElectionResultBallot> secondaryResults)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var electionResult = await LoadElectionResultWithCandidates(aggregate.PoliticalBusinessResultId);

        var contestId = await EnsureEditPermissionsForBundle(electionResult, aggregate, false);
        EnsureValidCandidates(electionResult, selectedCandidateIds);

        emptyVoteCount = CalculateAndValidateEmptyVoteCount(
            emptyVoteCount,
            electionResult.MajorityElection.NumberOfMandates,
            individualVoteCount,
            invalidVoteCount,
            aggregate.ResultEntryParams.AutomaticEmptyVoteCounting,
            selectedCandidateIds.Count);

        ValidateSecondaryResults(electionResult, secondaryResults);

        aggregate.UpdateBallot(ballotNumber, emptyVoteCount.Value, individualVoteCount, invalidVoteCount, selectedCandidateIds, secondaryResults, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task DeleteBallot(Guid bundleId, int ballotNumber)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate, false);
        aggregate.DeleteBallot(ballotNumber, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task BundleSubmissionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate, false);
        aggregate.SubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task BundleCorrectionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate, false);
        aggregate.CorrectionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task RejectBundleReview(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureReviewPermissionsForBundle(aggregate);
        aggregate.RejectReview(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task SucceedBundleReview(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureReviewPermissionsForBundle(aggregate);
        aggregate.SucceedReview(contestId);
        await AggregateRepository.Save(aggregate);
    }

    protected override async Task<DataModels.MajorityElectionResult> LoadPoliticalBusinessResult(Guid resultId)
    {
        return await _resultRepo.Query()
                   .Include(vr => vr.MajorityElection.Contest)
                   .Include(vr => vr.MajorityElection.DomainOfInfluence)
                   .FirstOrDefaultAsync(x => x.Id == resultId)
               ?? throw new EntityNotFoundException(resultId);
    }

    protected override Task<bool> DoesBundleExist(Guid id)
        => _bundleRepo.ExistsByKey(id);

    private async Task<DataModels.MajorityElectionResult> LoadElectionResultWithCandidates(Guid id)
    {
        return await _resultRepo.Query()
                   .AsSplitQuery()
                   .Include(x => x.MajorityElection.DomainOfInfluence)
                   .Include(x => x.MajorityElection.Contest)
                   .Include(x => x.CandidateResults)
                   .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                   .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.SecondaryMajorityElection)
                   .FirstOrDefaultAsync(x => x.Id == id)
               ?? throw new EntityNotFoundException(id);
    }

    private void EnsureValidCandidates(DataModels.MajorityElectionResult result, IReadOnlyCollection<Guid> suppliedCandidateIds)
    {
        EnsureValidCandidates(
            result.CandidateResults.Select(x => x.CandidateId),
            suppliedCandidateIds);
    }

    private void ValidateSecondaryResults(
        DataModels.MajorityElectionResult electionResult,
        IEnumerable<SecondaryMajorityElectionResultBallot> secondaryResults)
    {
        var secondaryElectionResultsByElectionId = electionResult.SecondaryMajorityElectionResults
            .ToDictionary(x => x.SecondaryMajorityElectionId);
        foreach (var secondaryResult in secondaryResults)
        {
            if (!secondaryElectionResultsByElectionId.TryGetValue(secondaryResult.SecondaryMajorityElectionId, out var secondaryElectionResult))
            {
                throw new EntityNotFoundException(secondaryResult.SecondaryMajorityElectionId);
            }

            EnsureValidCandidates(
                secondaryElectionResult.CandidateResults.Select(x => x.CandidateId),
                secondaryResult.SelectedCandidateIds);
            secondaryResult.EmptyVoteCount = CalculateAndValidateEmptyVoteCount(
                secondaryResult.EmptyVoteCount,
                secondaryElectionResult.SecondaryMajorityElection.NumberOfMandates,
                secondaryResult.IndividualVoteCount,
                secondaryResult.InvalidVoteCount,
                electionResult.EntryParams!.AutomaticEmptyVoteCounting,
                secondaryResult.SelectedCandidateIds.Count);
        }
    }

    private int CalculateAndValidateEmptyVoteCount(
        int? emptyVoteCount,
        int numberOfMandates,
        int individualVoteCount,
        int invalidVoteCount,
        bool automaticEmptyVoteCounting,
        int selectedCandidateCount)
    {
        var expectedEmptyVoteCount = numberOfMandates - selectedCandidateCount - individualVoteCount - invalidVoteCount;
        if (expectedEmptyVoteCount < 0)
        {
            throw new ValidationException("too many candidates provided");
        }

        if (!emptyVoteCount.HasValue)
        {
            if (!automaticEmptyVoteCounting)
            {
                throw new ValidationException("automatic empty vote counting is disabled");
            }

            return expectedEmptyVoteCount;
        }

        if (emptyVoteCount != expectedEmptyVoteCount)
        {
            throw new ValidationException($"wrong number of empty votes, expected: {expectedEmptyVoteCount} provided: {emptyVoteCount}");
        }

        return expectedEmptyVoteCount;
    }

    private void EnsureValidCandidates(
        IEnumerable<Guid> candidateIds,
        IReadOnlyCollection<Guid> suppliedCandidateIds)
    {
        if (suppliedCandidateIds.ToHashSet().Count != suppliedCandidateIds.Count)
        {
            throw new ValidationException("duplicated candidates provided");
        }

        if (suppliedCandidateIds.Except(candidateIds).Any())
        {
            throw new ValidationException("unknown candidates provided");
        }
    }
}
