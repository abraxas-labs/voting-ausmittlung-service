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

public class ProportionalElectionResultBundleWriter
    : PoliticalBusinessResultBundleWriter<DataModels.ProportionalElectionResult, ProportionalElectionResultBundleAggregate>
{
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionCandidate> _candidatesRepo;
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionList> _listsRepo;

    public ProportionalElectionResultBundleWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        IDbRepository<DataContext, DataModels.ProportionalElectionResult> resultRepo,
        IDbRepository<DataContext, DataModels.ProportionalElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, DataModels.ProportionalElectionCandidate> candidatesRepo,
        IDbRepository<DataContext, DataModels.ProportionalElectionList> listsRepo,
        PermissionService permissionService,
        ContestService contestService,
        IAuth auth,
        ISecondFactorTransactionService secondFactorService)
        : base(permissionService, contestService, auth, aggregateRepository, secondFactorService)
    {
        _aggregateFactory = aggregateFactory;
        _resultRepo = resultRepo;
        _candidatesRepo = candidatesRepo;
        _listsRepo = listsRepo;
        _bundleRepo = bundleRepo;
    }

    public async Task<ProportionalElectionResultBundleAggregate> CreateBundle(
        Guid resultId,
        Guid? listId,
        int? bundleNumber)
    {
        var contestId = await EnsurePoliticalBusinessPermissions(resultId);

        var electionResultAggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        await EnsureListExists(listId, electionResultAggregate.ProportionalElectionId);
        bundleNumber = EnsureBundleNumber(electionResultAggregate, bundleNumber, contestId);

        var aggregate = _aggregateFactory.New<ProportionalElectionResultBundleAggregate>();
        aggregate.Create(
            null,
            resultId,
            listId,
            bundleNumber.Value,
            electionResultAggregate.ResultEntry,
            contestId);

        await AggregateRepository.Save(electionResultAggregate);
        await AggregateRepository.Save(aggregate);
        return aggregate;
    }

    public async Task DeleteBundle(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.Delete(contestId);

        var electionResultAggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(aggregate.PoliticalBusinessResultId);
        electionResultAggregate.EnsureCanDeleteBundle(aggregate.BundleNumber);

        await AggregateRepository.Save(aggregate);
    }

    public async Task<int> CreateBallot(
        Guid bundleId,
        int? ballotNumber,
        int? emptyVoteCount,
        IReadOnlyCollection<ProportionalElectionResultBallotCandidate> candidates)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultBundleAggregate>(bundleId);
        var result = await LoadPoliticalBusinessResult(aggregate.PoliticalBusinessResultId);

        var contestId = await EnsureEditPermissionsForBundle(result, aggregate);
        await EnsureValidCandidates(candidates, result.ProportionalElectionId, aggregate.ListId);
        emptyVoteCount = CalculateAndValidateEmptyVoteCount(
            emptyVoteCount,
            result.ProportionalElection.NumberOfMandates,
            aggregate.ResultEntryParams.AutomaticEmptyVoteCounting,
            candidates);

        aggregate.CreateBallot(ballotNumber, emptyVoteCount.Value, candidates, contestId);
        await AggregateRepository.Save(aggregate);
        return aggregate.CurrentBallotNumber;
    }

    public async Task UpdateBallot(
        Guid bundleId,
        int ballotNumber,
        int? emptyVoteCount,
        IReadOnlyCollection<ProportionalElectionResultBallotCandidate> candidates)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultBundleAggregate>(bundleId);
        var result = await LoadPoliticalBusinessResult(aggregate.PoliticalBusinessResultId);

        var contestId = await EnsureEditPermissionsForBundle(result, aggregate);
        await EnsureValidCandidates(candidates, result.ProportionalElectionId, aggregate.ListId);
        emptyVoteCount = CalculateAndValidateEmptyVoteCount(
            emptyVoteCount,
            result.ProportionalElection.NumberOfMandates,
            aggregate.ResultEntryParams.AutomaticEmptyVoteCounting,
            candidates);

        aggregate.UpdateBallot(ballotNumber, emptyVoteCount.Value, candidates, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task DeleteBallot(Guid bundleId, int ballotNumber)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.DeleteBallot(ballotNumber, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task BundleSubmissionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.SubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task BundleCorrectionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.CorrectionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task RejectBundleReview(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultBundleAggregate>(bundleId);
        var contestId = await EnsureReviewPermissionsForBundle(aggregate, succeed: false);
        aggregate.RejectReview(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task SucceedBundleReview(IReadOnlyCollection<Guid> bundleIds)
    {
        await ExecuteOnAllAggregates<ProportionalElectionResultBundleAggregate>(bundleIds, async aggregate =>
        {
            var contestId = await EnsureReviewPermissionsForBundle(aggregate, succeed: true);
            aggregate.SucceedReview(contestId);
        });
    }

    public async Task ResetToSubmissionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<ProportionalElectionResultBundleAggregate>(bundleId);
        var result = await LoadPoliticalBusinessResult(aggregate.PoliticalBusinessResultId);

        if (result.State is not DataModels.CountingCircleResultState.SubmissionOngoing and not DataModels.CountingCircleResultState.ReadyForCorrection)
        {
            throw new ForbiddenException("Cannot reset the bundle state in while the political business result is in state: " + result.State);
        }

        var contestId = await EnsureEditPermissionsForBundle(result, aggregate);
        aggregate.ResetToSubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    protected override async Task<DataModels.ProportionalElectionResult> LoadPoliticalBusinessResult(Guid resultId)
    {
        return await _resultRepo.Query()
                   .Include(vr => vr.ProportionalElection.Contest)
                   .Include(vr => vr.ProportionalElection.DomainOfInfluence)
                   .FirstOrDefaultAsync(x => x.Id == resultId)
               ?? throw new EntityNotFoundException(resultId);
    }

    protected override Task<bool> DoesBundleExist(Guid id)
        => _bundleRepo.ExistsByKey(id);

    private async Task EnsureValidCandidates(
        IReadOnlyCollection<ProportionalElectionResultBallotCandidate> candidates,
        Guid proportionalElectionId,
        Guid? listId)
    {
        // validate a candidate is not more than 2 times on the ballot
        if (candidates.GroupBy(c => c.CandidateId).Any(g => g.Count() > 2))
        {
            throw new ValidationException("a candidate can be twice on the list at max");
        }

        var uniqueCandidateIds = candidates.Select(c => c.CandidateId).ToHashSet();
        if (uniqueCandidateIds.Count == 0)
        {
            if (!listId.HasValue)
            {
                throw new ValidationException("At least one candidate must be added.");
            }

            return;
        }

        // Fetch all supplied candidates (scoped to the election) in a single round trip, covering both the
        // "candidate exists and belongs to the election" and the "candidate belongs to the bundle's list" checks.
        var matchingCandidates = await _candidatesRepo.Query()
            .Where(x => uniqueCandidateIds.Contains(x.Id) && x.ProportionalElectionList.ProportionalElectionId == proportionalElectionId)
            .Select(x => new { x.Id, x.ProportionalElectionListId })
            .ToListAsync();

        var existingCandidateIds = matchingCandidates.Select(x => x.Id).ToHashSet();
        if (uniqueCandidateIds.Except(existingCandidateIds).Any())
        {
            throw new ValidationException("unknown candidates provided");
        }

        if (!listId.HasValue)
        {
            return;
        }

        // validate all on list candidates are part of the list of the bundle
        var uniqueCandidateOnListIds = candidates
            .Where(c => c.OnList)
            .Select(c => c.CandidateId)
            .ToHashSet();
        var uniqueListCandidateIds = matchingCandidates
            .Where(x => x.ProportionalElectionListId == listId)
            .Select(x => x.Id)
            .ToHashSet();
        var hasUnknownOnListCandidates = uniqueCandidateOnListIds.Except(uniqueListCandidateIds).Any();
        if (hasUnknownOnListCandidates)
        {
            throw new ValidationException("unknown list candidates provided");
        }
    }

    private int CalculateAndValidateEmptyVoteCount(
        int? emptyVoteCount,
        int numberOfMandates,
        bool automaticEmptyVoteCounting,
        IEnumerable<ProportionalElectionResultBallotCandidate> candidates)
    {
        var expectedEmptyVoteCount = numberOfMandates - candidates.Count();
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

    /// <summary>
    /// Validates that, if <paramref name="listId"/> is set, a list with that id exists and belongs to
    /// <paramref name="proportionalElectionId"/>, preventing a bundle from being created with a dangling
    /// reference that would later fail to be inserted (foreign key violation) during event processing.
    /// </summary>
    /// <exception cref="EntityNotFoundException">Thrown if no such list exists.</exception>
    private async Task EnsureListExists(Guid? listId, Guid proportionalElectionId)
    {
        if (!listId.HasValue)
        {
            return;
        }

        var listExists = await _listsRepo.Query()
            .AnyAsync(x => x.Id == listId.Value && x.ProportionalElectionId == proportionalElectionId);
        if (!listExists)
        {
            throw new EntityNotFoundException(listId.Value);
        }
    }

    /// <summary>
    /// Determines the bundle number for a new bundle: generates it via the aggregate if automatic bundle
    /// number generation is enabled, otherwise validates that <paramref name="bundleNumber"/> was provided
    /// and registers it on the aggregate.
    /// </summary>
    /// <exception cref="ValidationException">Thrown if automatic generation is disabled and no bundle number was provided.</exception>
    private int EnsureBundleNumber(ProportionalElectionResultAggregate electionResultAggregate, int? bundleNumber, Guid contestId)
    {
        if (electionResultAggregate.ResultEntry.AutomaticBallotBundleNumberGeneration)
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
