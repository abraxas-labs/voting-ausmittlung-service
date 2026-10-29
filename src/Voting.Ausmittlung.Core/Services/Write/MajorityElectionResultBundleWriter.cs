// (c) Copyright by Abraxas Informatik AG
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
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.SecondFactor.Services;
using Voting.Lib.Iam.Store;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class MajorityElectionResultBundleWriter
    : PoliticalBusinessResultBundleWriter<DataModels.MajorityElectionResult, MajorityElectionResultBundleAggregate>
{
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionBallotGroupEntryCandidate> _ballotGroupEntryCandidateRepo;
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionBallotGroup> _ballotGroupRepo;

    public MajorityElectionResultBundleWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        IDbRepository<DataContext, DataModels.MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, DataModels.MajorityElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, DataModels.MajorityElectionBallotGroupEntryCandidate> ballotGroupEntryCandidateRepo,
        IDbRepository<DataContext, DataModels.MajorityElectionBallotGroup> ballotGroupRepo,
        PermissionService permissionService,
        ContestService contestService,
        IAuth auth,
        ISecondFactorTransactionService secondFactorService)
        : base(permissionService, contestService, auth, aggregateRepository, secondFactorService)
    {
        _aggregateFactory = aggregateFactory;
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotGroupEntryCandidateRepo = ballotGroupEntryCandidateRepo;
        _ballotGroupRepo = ballotGroupRepo;
    }

    public async Task<MajorityElectionResultBundleAggregate> CreateBundle(
        Guid resultId,
        int? bundleNumber,
        Guid? ballotGroupId)
    {
        var contestId = await EnsurePoliticalBusinessPermissions(resultId);

        var electionResultAggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(resultId);
        await EnsureBallotGroupExists(ballotGroupId, electionResultAggregate.MajorityElectionId);
        bundleNumber = EnsureBundleNumber(electionResultAggregate, bundleNumber, contestId);

        var aggregate = _aggregateFactory.New<MajorityElectionResultBundleAggregate>();
        aggregate.Create(
            null,
            resultId,
            bundleNumber.Value,
            electionResultAggregate.ResultEntry,
            electionResultAggregate.ResultEntryParams,
            contestId,
            ballotGroupId);

        await AggregateRepository.Save(electionResultAggregate);
        await AggregateRepository.Save(aggregate);
        return aggregate;
    }

    public async Task DeleteBundle(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.Delete(contestId);

        var electionResultAggregate = await AggregateRepository.GetById<MajorityElectionResultAggregate>(aggregate.PoliticalBusinessResultId);
        electionResultAggregate.EnsureCanDeleteBundle(aggregate.BundleNumber);

        await AggregateRepository.Save(aggregate);
    }

    public async Task<int> CreateBallot(
        Guid bundleId,
        int? ballotNumber,
        int? emptyVoteCount,
        int individualVoteCount,
        int invalidVoteCount,
        IReadOnlyCollection<MajorityElectionResultBallotCandidate> candidates,
        IReadOnlyCollection<SecondaryMajorityElectionResultBallot> secondaryResults)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var electionResult = await LoadElectionResultWithCandidates(aggregate.PoliticalBusinessResultId);

        var contestId = await EnsureEditPermissionsForBundle(electionResult, aggregate);
        await EnsureValidCandidates(electionResult, candidates, aggregate.BallotGroupId);
        EnsureNoEmptyVoteCountForSingleMandate(electionResult, emptyVoteCount);

        emptyVoteCount = CalculateAndValidateEmptyVoteCount(
            emptyVoteCount,
            electionResult.MajorityElection.NumberOfMandates,
            individualVoteCount,
            invalidVoteCount,
            aggregate.ResultEntryParams.AutomaticEmptyVoteCounting,
            candidates.Count);

        EnsureNoDisabledVoteCount(individualVoteCount, electionResult.MajorityElection);
        await ValidateSecondaryResults(electionResult, secondaryResults, candidates.Select(x => x.CandidateId).ToList(), aggregate.BallotGroupId);

        aggregate.CreateBallot(ballotNumber, emptyVoteCount.Value, individualVoteCount, invalidVoteCount, candidates, secondaryResults, contestId);
        await AggregateRepository.Save(aggregate);
        return aggregate.CurrentBallotNumber;
    }

    public async Task UpdateBallot(
        Guid bundleId,
        int ballotNumber,
        int? emptyVoteCount,
        int individualVoteCount,
        int invalidVoteCount,
        IReadOnlyCollection<MajorityElectionResultBallotCandidate> candidates,
        IReadOnlyCollection<SecondaryMajorityElectionResultBallot> secondaryResults)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var electionResult = await LoadElectionResultWithCandidates(aggregate.PoliticalBusinessResultId);

        var contestId = await EnsureEditPermissionsForBundle(electionResult, aggregate);
        await EnsureValidCandidates(electionResult, candidates, aggregate.BallotGroupId);
        EnsureNoEmptyVoteCountForSingleMandate(electionResult, emptyVoteCount);

        emptyVoteCount = CalculateAndValidateEmptyVoteCount(
            emptyVoteCount,
            electionResult.MajorityElection.NumberOfMandates,
            individualVoteCount,
            invalidVoteCount,
            aggregate.ResultEntryParams.AutomaticEmptyVoteCounting,
            candidates.Count);

        EnsureNoDisabledVoteCount(individualVoteCount, electionResult.MajorityElection);
        await ValidateSecondaryResults(electionResult, secondaryResults, candidates.Select(x => x.CandidateId).ToList(), aggregate.BallotGroupId);

        aggregate.UpdateBallot(ballotNumber, emptyVoteCount.Value, individualVoteCount, invalidVoteCount, candidates, secondaryResults, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task DeleteBallot(Guid bundleId, int ballotNumber)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.DeleteBallot(ballotNumber, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task BundleSubmissionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.SubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task BundleCorrectionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.CorrectionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task RejectBundleReview(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureReviewPermissionsForBundle(aggregate, succeed: false);
        aggregate.RejectReview(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task SucceedBundleReview(IReadOnlyCollection<Guid> bundleIds)
    {
        await ExecuteOnAllAggregates<MajorityElectionResultBundleAggregate>(bundleIds, async aggregate =>
        {
            var contestId = await EnsureReviewPermissionsForBundle(aggregate, succeed: true);
            aggregate.SucceedReview(contestId);
        });
    }

    public async Task ResetToSubmissionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<MajorityElectionResultBundleAggregate>(bundleId);
        var result = await LoadPoliticalBusinessResult(aggregate.PoliticalBusinessResultId);

        if (result.State is not DataModels.CountingCircleResultState.SubmissionOngoing and not DataModels.CountingCircleResultState.ReadyForCorrection)
        {
            throw new ForbiddenException("Cannot reset the bundle state in while the political business result is in state: " + result.State);
        }

        var contestId = await EnsureEditPermissionsForBundle(result, aggregate);
        aggregate.ResetToSubmissionFinished(contestId);
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
                   .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults).ThenInclude(c => c.Candidate)
                   .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.SecondaryMajorityElection)
                   .FirstOrDefaultAsync(x => x.Id == id)
               ?? throw new EntityNotFoundException(id);
    }

    private async Task ValidateSecondaryResults(
        DataModels.MajorityElectionResult electionResult,
        IEnumerable<SecondaryMajorityElectionResultBallot> secondaryResults,
        IReadOnlyCollection<Guid> primaryElectionSelectedCandidateIds,
        Guid? ballotGroupId)
    {
        var secondaryElectionResultsByElectionId = electionResult.SecondaryMajorityElectionResults
            .ToDictionary(x => x.SecondaryMajorityElectionId);
        foreach (var secondaryResult in secondaryResults)
        {
            if (!secondaryElectionResultsByElectionId.TryGetValue(secondaryResult.SecondaryMajorityElectionId, out var secondaryElectionResult))
            {
                throw new EntityNotFoundException(secondaryResult.SecondaryMajorityElectionId);
            }

            await EnsureValidCandidates(
                secondaryElectionResult.CandidateResults.Select(x => x.CandidateId),
                secondaryResult.Candidates,
                ballotGroupId);

            EnsureSelectedCandidatesAreSelectedInPrimaryElection(
                secondaryElectionResult,
                secondaryResult,
                primaryElectionSelectedCandidateIds);

            secondaryResult.EmptyVoteCount = CalculateAndValidateEmptyVoteCount(
                secondaryResult.EmptyVoteCount,
                secondaryElectionResult.SecondaryMajorityElection.NumberOfMandates,
                secondaryResult.IndividualVoteCount,
                secondaryResult.InvalidVoteCount,
                electionResult.EntryParams!.AutomaticEmptyVoteCounting,
                secondaryResult.Candidates.Count);

            EnsureNoDisabledVoteCount(secondaryResult.IndividualVoteCount, secondaryElectionResult.SecondaryMajorityElection);
        }
    }

    private void EnsureSelectedCandidatesAreSelectedInPrimaryElection(
        DataModels.SecondaryMajorityElectionResult secondaryElectionResult,
        SecondaryMajorityElectionResultBallot secondaryResult,
        IReadOnlyCollection<Guid> primaryElectionSelectedCandidateIds)
    {
        var candidatesById = secondaryElectionResult.CandidateResults
            .Select(x => x.Candidate)
            .ToDictionary(x => x.Id, x => x);
        foreach (var secondarySelectedCandidate in secondaryResult.Candidates)
        {
            var refId = candidatesById[secondarySelectedCandidate.CandidateId].CandidateReferenceId;
            if (refId.HasValue && !primaryElectionSelectedCandidateIds.Contains(refId.Value))
            {
                throw new SecondaryMajorityElectionCandidateNotSelectedInPrimaryElectionException();
            }
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
            if (!automaticEmptyVoteCounting && numberOfMandates > 1)
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

    private void EnsureNoDisabledVoteCount(
        int individualVoteCount,
        DataModels.MajorityElectionBase election)
    {
        if (election.IndividualCandidatesDisabled && individualVoteCount > 0)
        {
            throw new ValidationException($"Individual vote count is disabled on election {election.Id}");
        }
    }

    private async Task EnsureValidCandidates(DataModels.MajorityElectionResult result, IReadOnlyCollection<MajorityElectionResultBallotCandidate> suppliedCandidates, Guid? ballotGroupId)
    {
        await EnsureValidCandidates(
            result.CandidateResults.Select(x => x.CandidateId),
            suppliedCandidates,
            ballotGroupId);
    }

    private async Task EnsureValidCandidates(
        IEnumerable<Guid> candidateIds,
        IReadOnlyCollection<MajorityElectionResultBallotCandidate> suppliedCandidates,
        Guid? ballotGroupId)
    {
        var suppliedCandidateIds = suppliedCandidates.Select(x => x.CandidateId).ToList();
        if (suppliedCandidateIds.ToHashSet().Count != suppliedCandidateIds.Count)
        {
            throw new ValidationException("duplicated candidates provided");
        }

        if (suppliedCandidateIds.Except(candidateIds).Any())
        {
            throw new ValidationException("unknown candidates provided");
        }

        // validate all on ballot group candidates are part of the ballot group of the bundle
        var uniqueCandidateOnBallotGroupIds = suppliedCandidates
            .Where(c => c.OnBallotGroup)
            .Select(c => c.CandidateId)
            .ToHashSet();
        var ballotGroupCandidateIds = await _ballotGroupEntryCandidateRepo.Query()
            .Where(x => x.BallotGroupEntry.BallotGroupId == ballotGroupId)
            .Select(x => x.PrimaryElectionCandidateId ?? x.SecondaryElectionCandidateId!.Value)
            .ToHashSetAsync();
        var hasUnknownOnBallotGroupCandidates = uniqueCandidateOnBallotGroupIds.Except(ballotGroupCandidateIds).Any();
        if (hasUnknownOnBallotGroupCandidates)
        {
            throw new ValidationException("unknown ballot group candidates provided");
        }
    }

    private void EnsureNoEmptyVoteCountForSingleMandate(DataModels.MajorityElectionResult electionResult, int? emptyVoteCount)
    {
        if (electionResult.MajorityElection.NumberOfMandates == 1 && emptyVoteCount != null && !electionResult.SecondaryMajorityElectionResults.Any())
        {
            throw new ValidationException("empty vote count provided with single mandate and no other elections on the same ballot");
        }
    }

    /// <summary>
    /// Validates that, if <paramref name="ballotGroupId"/> is set, a ballot group with that id exists and
    /// belongs to <paramref name="majorityElectionId"/>, preventing a bundle from being created with a
    /// dangling reference that would later fail to be inserted (foreign key violation) during event processing.
    /// </summary>
    /// <exception cref="EntityNotFoundException">Thrown if no such ballot group exists.</exception>
    private async Task EnsureBallotGroupExists(Guid? ballotGroupId, Guid majorityElectionId)
    {
        if (!ballotGroupId.HasValue)
        {
            return;
        }

        var ballotGroupExists = await _ballotGroupRepo.Query()
            .AnyAsync(x => x.Id == ballotGroupId.Value && x.MajorityElectionId == majorityElectionId);
        if (!ballotGroupExists)
        {
            throw new EntityNotFoundException(ballotGroupId.Value);
        }
    }

    /// <summary>
    /// Determines the bundle number for a new bundle: generates it via the aggregate if automatic bundle
    /// number generation is enabled, otherwise validates that <paramref name="bundleNumber"/> was provided
    /// and registers it on the aggregate.
    /// </summary>
    /// <exception cref="ValidationException">Thrown if automatic generation is disabled and no bundle number was provided.</exception>
    private int EnsureBundleNumber(MajorityElectionResultAggregate electionResultAggregate, int? bundleNumber, Guid contestId)
    {
        if (electionResultAggregate.ResultEntryParams.AutomaticBallotBundleNumberGeneration)
        {
            return electionResultAggregate.GenerateBundleNumber(contestId);
        }

        if (bundleNumber.HasValue)
        {
            electionResultAggregate.BundleNumberEntered(bundleNumber.Value, contestId);
            return bundleNumber.Value;
        }

        throw new ValidationException("bundle number is not generated automatically and should be provided");
    }
}
