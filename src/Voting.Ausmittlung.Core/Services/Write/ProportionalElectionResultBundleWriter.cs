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

    public ProportionalElectionResultBundleWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        IDbRepository<DataContext, DataModels.ProportionalElectionResult> resultRepo,
        IDbRepository<DataContext, DataModels.ProportionalElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, DataModels.ProportionalElectionCandidate> candidatesRepo,
        PermissionService permissionService,
        ContestService contestService,
        IAuth auth,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
        : base(permissionService, contestService, auth, aggregateRepository, secondFactorTransactionWriter)
    {
        _aggregateFactory = aggregateFactory;
        _resultRepo = resultRepo;
        _candidatesRepo = candidatesRepo;
        _bundleRepo = bundleRepo;
    }

    public async Task<ProportionalElectionResultBundleAggregate> CreateBundle(
        Guid resultId,
        Guid? listId,
        int? bundleNumber)
    {
        var contestId = await EnsurePoliticalBusinessPermissions(resultId);

        var electionResultAggregate = await AggregateRepository.GetById<ProportionalElectionResultAggregate>(resultId);
        var generateBundleNumber = electionResultAggregate.ResultEntry.AutomaticBallotBundleNumberGeneration;
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
        electionResultAggregate.FreeBundleNumber(aggregate.BundleNumber, contestId);

        await AggregateRepository.Save(electionResultAggregate);
        await AggregateRepository.Save(aggregate);
    }

    public async Task<int> CreateBallot(
        Guid bundleId,
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

        aggregate.CreateBallot(emptyVoteCount.Value, candidates, contestId);
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
        var contestId = await EnsureReviewPermissionsForBundle(aggregate);
        aggregate.RejectReview(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task SucceedBundleReview(IReadOnlyCollection<Guid> bundleIds)
    {
        await ExecuteOnAllAggregates<ProportionalElectionResultBundleAggregate>(bundleIds, async aggregate =>
        {
            var contestId = await EnsureReviewPermissionsForBundle(aggregate);
            aggregate.SucceedReview(contestId);
        });
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

        // validate all candidates are known
        var candidateIds = candidates.Select(c => c.CandidateId).ToList();
        var uniqueCandidateIds = candidateIds.ToHashSet();
        var hasUnknownCandidates = await _candidatesRepo.Query()
            .AnyAsync(x => uniqueCandidateIds.Contains(x.Id) && x.ProportionalElectionList.ProportionalElectionId != proportionalElectionId);
        if (hasUnknownCandidates)
        {
            throw new ValidationException("unknown candidates provided");
        }

        if (!listId.HasValue)
        {
            if (uniqueCandidateIds.Count == 0)
            {
                throw new ValidationException("At least one candidate must be added.");
            }

            return;
        }

        // validate all on list candidates are part of the list of the bundle
        var uniqueCandidateOnListIds = candidates
            .Where(c => c.OnList)
            .Select(c => c.CandidateId)
            .ToHashSet();
        var listCandidates = await _candidatesRepo.Query()
            .Where(x => x.ProportionalElectionListId == listId)
            .Select(x => new { x.Id, x.Accumulated })
            .ToListAsync();
        var uniqueListCandidateIds = listCandidates.Select(x => x.Id).ToHashSet();
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
}
